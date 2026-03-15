using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using InventoryKpiSystem.Core.Models;

namespace InventoryKpiSystem.Core.DataAccess
{
	/// <summary>
	/// Đọc file JSON từ Xero API và chuyển đổi sang Core models.
	///
	/// XERO FORMAT:
	///   ACCREC (Type=ACCREC) → Invoice (hàng bán ra)
	///   ACCPAY (Type=ACCPAY) → PurchaseOrder (hàng nhập vào)
	///   Items               → Product
	///
	/// Vì LineItems trong Xero không có ItemCode, dùng Description
	/// đã chuẩn hoá làm ProductId.
	/// </summary>
	public class XeroDataImporter
	{
		// ─── Xero JSON models ────────────────────────────────────────────

		private class XeroInvoiceFile
		{
			public List<XeroInvoice> Invoices { get; set; } = new();
		}

		private class XeroInvoice
		{
			public string Type { get; set; } = "";
			public string InvoiceID { get; set; } = "";
			public string InvoiceNumber { get; set; } = "";
			public string DateString { get; set; } = "";
			public string Status { get; set; } = "";
			public List<XeroLineItem> LineItems { get; set; } = new();
		}

		private class XeroLineItem
		{
			public string LineItemID { get; set; } = "";
			public string Description { get; set; } = "";
			public string? ItemCode { get; set; }
			public string AccountCode { get; set; } = "";
			public double Quantity { get; set; }
			public double UnitAmount { get; set; }
		}

		private class XeroItemFile
		{
			public List<XeroItem> Items { get; set; } = new();
		}

		private class XeroItem
		{
			public string ItemID { get; set; } = "";
			public string Code { get; set; } = "";
			public string Name { get; set; } = "";
			public string Description { get; set; } = "";
			public bool IsSold { get; set; }
			public bool IsPurchased { get; set; }
			public bool IsTrackedAsInventory { get; set; }
			public XeroPriceDetails? PurchaseDetails { get; set; }
			public XeroPriceDetails? SalesDetails { get; set; }
		}

		private class XeroPriceDetails
		{
			public double UnitPrice { get; set; }
			public string AccountCode { get; set; } = "";
			public string TaxType { get; set; } = "";
		}

		// ─── Public API ──────────────────────────────────────────────────

		/// <summary>
		/// Đọc một file Xero invoice JSON, trả về:
		///   invoices       = ACCREC lines  → List[Invoice]
		///   purchaseOrders = ACCPAY lines  → List[PurchaseOrder]
		/// </summary>
		public async Task<(List<Invoice> invoices, List<PurchaseOrder> purchaseOrders)>
			ReadInvoiceFileAsync(string filePath)
		{
			var invoices = new List<Invoice>();
			var purchaseOrders = new List<PurchaseOrder>();

			if (!File.Exists(filePath))
				return (invoices, purchaseOrders);

			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			await using var stream = File.OpenRead(filePath);
			var file = await JsonSerializer.DeserializeAsync<XeroInvoiceFile>(stream, options);

			if (file?.Invoices == null) return (invoices, purchaseOrders);

			foreach (var inv in file.Invoices)
			{
				// Bỏ qua draft / void
				if (inv.Status is "DRAFT" or "VOIDED" or "DELETED") continue;

				var date = ParseDate(inv.DateString);

				foreach (var li in inv.LineItems)
				{
					if (li.Quantity <= 0 || li.UnitAmount < 0) continue;

					// Dùng ItemCode nếu có, không thì dùng Description chuẩn hoá
					var productId = !string.IsNullOrWhiteSpace(li.ItemCode)
						? li.ItemCode.Trim()
						: NormalizeDescription(li.Description);

					if (string.IsNullOrEmpty(productId)) continue;

					if (inv.Type == "ACCREC")
					{
						invoices.Add(new Invoice
						{
							InvoiceId = li.LineItemID,
							ProductId = productId,
							Quantity = (int)Math.Round(li.Quantity),
							UnitPrice = (decimal)li.UnitAmount,
							Date = date
						});
					}
					else if (inv.Type == "ACCPAY")
					{
						purchaseOrders.Add(new PurchaseOrder
						{
							OrderId = li.LineItemID,
							ProductId = productId,
							Quantity = (int)Math.Round(li.Quantity),
							UnitCost = (decimal)li.UnitAmount,
							Date = date
						});
					}
				}
			}

			return (invoices, purchaseOrders);
		}

		/// <summary>
		/// Đọc file Xero Items JSON, trả về List[Product]
		/// </summary>
		public async Task<List<Product>> ReadProductFileAsync(string filePath)
		{
			var products = new List<Product>();
			if (!File.Exists(filePath)) return products;

			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			await using var stream = File.OpenRead(filePath);
			var file = await JsonSerializer.DeserializeAsync<XeroItemFile>(stream, options);

			if (file?.Items == null) return products;

			foreach (var item in file.Items)
			{
				products.Add(new Product
				{
					ItemID = item.ItemID,
					Code = item.Code,
					Name = item.Name,
					Description = item.Description,
					IsSold = item.IsSold,
					IsPurchased = item.IsPurchased,
					PurchaseDetails = item.PurchaseDetails != null ? new PriceDetails
					{
						UnitPrice = (decimal)item.PurchaseDetails.UnitPrice,
						AccountCode = item.PurchaseDetails.AccountCode,
						TaxType = item.PurchaseDetails.TaxType
					} : null,
					SalesDetails = item.SalesDetails != null ? new PriceDetails
					{
						UnitPrice = (decimal)item.SalesDetails.UnitPrice,
						AccountCode = item.SalesDetails.AccountCode,
						TaxType = item.SalesDetails.TaxType
					} : null
				});
			}

			return products;
		}

		/// <summary>
		/// Đọc toàn bộ thư mục chứa các file invoice Xero
		/// </summary>
		public async Task<(List<Invoice> invoices, List<PurchaseOrder> purchaseOrders)>
			ReadAllInvoiceFilesAsync(string directory)
		{
			var allInvoices = new List<Invoice>();
			var allOrders = new List<PurchaseOrder>();

			if (!Directory.Exists(directory)) return (allInvoices, allOrders);

			// Đọc cả .json và .txt (Xero export dạng txt)
			var files = new List<string>();
			files.AddRange(Directory.GetFiles(directory, "*.json"));
			files.AddRange(Directory.GetFiles(directory, "*.txt"));

			foreach (var file in files)
			{
				var (inv, po) = await ReadInvoiceFileAsync(file);
				allInvoices.AddRange(inv);
				allOrders.AddRange(po);
			}

			return (allInvoices, allOrders);
		}

		// ─── Helpers ─────────────────────────────────────────────────────

		private static DateTime ParseDate(string dateString)
		{
			if (DateTime.TryParse(dateString, out var dt))
				return dt;
			return DateTime.Today;
		}

		/// <summary>
		/// Chuẩn hoá Description thành ProductId ngắn gọn.
		/// Ví dụ: "254.1 15 litre Chlorine" → "15 litre Chlorine"
		///         "Pool Service 93.5"       → "Pool Service"
		/// </summary>
		private static string NormalizeDescription(string description)
		{
			if (string.IsNullOrWhiteSpace(description)) return "";

			var s = description.Trim();

			// Bỏ prefix dạng "NNN.N " (số contact + số line của Xero export)
			s = Regex.Replace(s, @"^\d+\.\d+\s+", "");

			// Bỏ số tiền ở cuối dạng " 93.5"
			s = Regex.Replace(s, @"\s+\d+(\.\d+)?$", "");

			// Giới hạn độ dài 60 ký tự
			if (s.Length > 60) s = s[..60].TrimEnd();

			return s.Trim();
		}
	}
}