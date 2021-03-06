using System;
using Dapper;
using FluentAssertions;
using UzEx.Dapper.Oracle.Tests.IntegrationTests.Util;
using UzEx.Dapper.Oracle.TypeHandler;
using Xunit;

namespace UzEx.Dapper.Oracle.Tests.IntegrationTests
{
    [Collection("OracleDocker")]
    public class GuidTypeMapperTests
    {
        private readonly Guid _customerId = Guid.NewGuid();

        public GuidTypeMapperTests(DatabaseFixture fixture)
        {
            Fixture = fixture;
            OracleTypeMapper.AddTypeHandler<Guid>(new GuidRaw16TypeHandler());

            var columns = new[]
            {
                new TableColumn {Name = "CustomerId", DataType = OracleMappingType.Raw, Size = 16, PrimaryKey = true},
                new TableColumn {Name = "Name", DataType = OracleMappingType.Varchar2, Size = 40},
                new TableColumn {Name = "City", DataType = OracleMappingType.Varchar2, Size = 40},
                new TableColumn {Name = "OtherGuid", DataType = OracleMappingType.Raw, Size = 16, Nullable = true}
            };

            TableCreator.Create(Fixture.Connection, "GuidCustomerTest", columns);
            InsertCustomer(new Customer {CustomerId = _customerId, Name = "DIPS AS", City = "Oslo"});
        }

        private DatabaseFixture Fixture { get; }

        private int InsertCustomer(Customer customer)
        {
            var sql =
                "INSERT INTO GuidCustomerTest(CustomerId,Name,City) VALUES(:CustomerId,:Name,:City)";
            var param = new OracleDynamicParameters();
            param.Add("CustomerId", customer.CustomerId);
            param.Add("Name", customer.Name);
            param.Add("City", customer.City);

            return Fixture.Connection.Execute(sql, param);
        }


        [Fact]
        [Trait("Category", "Integration")]
        public void InsertGuidTestTest()
        {
            var rowCount = InsertCustomer(new Customer
                {CustomerId = Guid.NewGuid(), Name = "DIPS AS", City = "Narvik"});
            rowCount.Should().Be(1);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void SelectGuidTest()
        {
            var selectParam = new OracleDynamicParameters();
            selectParam.Add("CustomerId", _customerId);

            var customer = Fixture.Connection.QuerySingle<Customer>(
                "SELECT CustomerId,Name,City,OtherGuid FROM GuidCustomerTest WHERE CustomerId=:CustomerId",
                selectParam);
            customer.CustomerId.Should().Be(_customerId);
            customer.Name.Should().Be("DIPS AS");
            customer.City.Should().Be("Oslo");
            customer.OtherGuid.Should().BeEmpty();
        }
    }

    public class Customer
    {
        public Guid CustomerId { get; set; }
        public string Name { get; set; }
        public string City { get; set; }
        public Guid OtherGuid { get; set; }
    }
}