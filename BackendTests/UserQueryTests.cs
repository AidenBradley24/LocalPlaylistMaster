using LocalPlaylistMaster.Backend;
using Xunit.Abstractions;
using Xunit;

namespace BackendTest
{
    public class UserQueryTests
    {
        private readonly ITestOutputHelper output;
        public UserQueryTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        private void Test(string TEST, string RESULT, object[] PARAMS)
        {
            var query = new UserQuery();
            query.Parse(TEST);
            Assert.Equal(RESULT, query.GetSQL());

            var outParams = query.Parameters();
            var parmEnumerator = outParams.GetEnumerator();

            for (int i = 0; i < PARAMS.Length; i++)
            {
                parmEnumerator.MoveNext();
                var outParam = parmEnumerator.Current;
                object param = PARAMS[i];
                Assert.Equal(param, outParam.Value);
            }
        }

        private void Fail(string TEST)
        {
            var query = new UserQuery();
            var ex = Assert.Throws<InvalidUserQueryException>(() => 
            {
                query.Parse(TEST);
                output.WriteLine(query.GetSQL());
            });
            output.WriteLine(ex.Message);
        }

        [Fact]
        public void Test01()
        {
            const string TEST = "1";
            const string RESULT = "Id = @p0";
            object[] PARAMS = [1];
            Test(TEST, RESULT, PARAMS);
        }

        [Fact]
        public void Test02()
        {
            const string TEST = "1, 2";
            const string RESULT = "Id = @p0 OR Id = @p1";
            object[] PARAMS = [1, 2];
            Test(TEST, RESULT, PARAMS);
        }

        [Fact]
        public void Test03()
        {
            const string TEST = "1-5";
            const string RESULT = "(Id BETWEEN @p0 AND @p1)";
            object[] PARAMS = [1, 5];
            Test(TEST, RESULT, PARAMS);
        }

        [Fact]
        public void Test04()
        {
            const string TEST = "remote=25";
            const string RESULT = "Remote = @p0";
            object[] PARAMS = [25];
            Test(TEST, RESULT, PARAMS);
        }

        [Fact]
        public void Test05()
        {
            const string TEST = "remote:25-30,rating=2";
            const string RESULT = "(Remote BETWEEN @p0 AND @p1) OR Rating = @p2";
            object[] PARAMS = [25, 30, 2];
            Test(TEST, RESULT, PARAMS);
        }

        [Fact]
        public void Test06()
        {
            const string TEST = "rating>=4";
            const string RESULT = "Rating >= @p0";
            object[] PARAMS = [4];
            Test(TEST, RESULT, PARAMS);
        }

        [Fact]
        public void Test07()
        {
            const string TEST = "album=\"test\"";
            const string RESULT = "Album = @p0 COLLATE NOCASE";
            object[] PARAMS = ["test"];
            Test(TEST, RESULT, PARAMS);
        }

        [Fact]
        public void Test08()
        {
            const string TEST = "album$\"soundtrack\"";
            const string RESULT = "Album LIKE @p0 ESCAPE '\\' COLLATE NOCASE";
            object[] PARAMS = ["%soundtrack"];
            Test(TEST, RESULT, PARAMS);
        }

        [Fact]
        public void Test09()
        {
            const string TEST = "name!^\"beans\" , 2";
            const string RESULT = "(NOT Name LIKE @p0 ESCAPE '\\' COLLATE NOCASE) OR Id = @p1";
            object[] PARAMS = ["beans%", 2];
            Test(TEST, RESULT, PARAMS);
        }

        [Fact]
        public void Test10()
        {
            const string TEST = "description*\"potato%\\\"\"";
            const string RESULT = "Description LIKE @p0 ESCAPE '\\' COLLATE NOCASE";
            object[] PARAMS = ["%potato\\%\"%"];
            Test(TEST, RESULT, PARAMS);
        }

        [Fact]
        public void Test11()
        {
            const string TEST = "album$\"ost\", name$\"soundtrack\"";
            const string RESULT = "Album LIKE @p0 ESCAPE '\\' COLLATE NOCASE OR Name LIKE @p1 ESCAPE '\\' COLLATE NOCASE";
            object[] PARAMS = ["%ost", "%soundtrack"];
            Test(TEST, RESULT, PARAMS);
        }

        [Fact]
        public void Test12()
        {
            const string TEST = "album$\"Soundtrack\"&name*\"theme\"";
            const string RESULT = "Album LIKE @p0 ESCAPE '\\' COLLATE NOCASE AND Name LIKE @p1 ESCAPE '\\' COLLATE NOCASE";
            object[] PARAMS = ["%Soundtrack", "%theme%"];
            Test(TEST, RESULT, PARAMS);
        }

        [Fact]
        public void Test13()
        {
            const string TEST = "id<10&remote=-1, 12";
            const string RESULT = "Id < @p0 AND Remote = @p1 OR Id = @p2";
            object[] PARAMS = [10, -1, 12];
            Test(TEST, RESULT, PARAMS);
        }

        [Fact]
        public void Test14()
        {
            const string TEST = "name=\"test\"&remote=-1, 12";
            const string RESULT = "Name = @p0 COLLATE NOCASE AND Remote = @p1 OR Id = @p2";
            object[] PARAMS = ["test", -1, 12];
            Test(TEST, RESULT, PARAMS);
        }


        [Fact]
        public void Test15()
        {
            const string TEST = "name!=\"\\\"%\"";
            const string RESULT = "(NOT Name = @p0 COLLATE NOCASE)";
            object[] PARAMS = ["\"%"];
            Test(TEST, RESULT, PARAMS);
        }

        [Fact]
        public void Test16()
        {
            const string TEST = "name!*\"\\\"%-\"";
            const string RESULT = "(NOT Name LIKE @p0 ESCAPE '\\' COLLATE NOCASE)";
            object[] PARAMS = ["%\"\\%\\-%"];
            Test(TEST, RESULT, PARAMS);
        }

        [Fact]
        public void Fail01()
        {
            const string TEST = "dsfjgoif";
            Fail(TEST);
        }

        [Fact]
        public void Fail02()
        {
            const string TEST = "-";
            Fail(TEST);
        }

        [Fact]
        public void Fail03()
        {
            const string TEST = "id:345";
            Fail(TEST);
        }

        [Fact]
        public void Fail04()
        {
            const string TEST = "id:345-";
            Fail(TEST);
        }

        [Fact]
        public void Fail05()
        {
            const string TEST = "id=\"hi\"";
            Fail(TEST);
        }

        [Fact]
        public void Fail06()
        {
            const string TEST = "name=2";
            Fail(TEST);
        }

        [Fact]
        public void Fail07()
        {
            const string TEST = "name>10";
            Fail(TEST);
        }

        [Fact]
        public void Fail08()
        {
            const string TEST = "id>1s50";
            Fail(TEST);
        }

        [Fact]
        public void Fail09()
        {
            const string TEST = ">";
            Fail(TEST);
        }

        [Fact]
        public void Fail10()
        {
            const string TEST = "=10";
            Fail(TEST);
        }
    }
}
