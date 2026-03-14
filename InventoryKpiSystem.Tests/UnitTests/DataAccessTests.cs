using InventoryKpiSystem.Core.DataAccess;
using Xunit;

namespace InventoryKpiSystem.Tests.UnitTests
{
    public class DataAccessTests
    {
        [Fact]
        public void Reader_FileNotFound_ReturnsNull()
        {
            var reader = new JsonDataReader();
            var result = reader.ReadDataAsync<object>("non_existent.json").Result;
            Assert.Null(result);
        }
    }
}