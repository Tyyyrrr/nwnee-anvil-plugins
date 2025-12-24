using System;
using Anvil.API;


namespace CharacterIdentity
{
    internal sealed class IdentityInfo : IEquatable<IdentityInfo>
    {
        public readonly int ID;


        private Identity _identity;
        internal Identity Identity
        {
            get => _identity;
            set
            {
                Name = value.LastName == string.Empty ? value.FirstName : value.FirstName + " " + value.LastName;
                _identity = value;
            }
        }


        public string Name { get; private set; } = string.Empty;


        public IdentityInfo(int id, Identity identity)
        {
            ID = id;
            Identity = identity;
        }
        

        public bool Equals(IdentityInfo? other) => other != null && other.ID == ID;
        public override bool Equals(object? obj) => obj is IdentityInfo other && Equals(other);
        public override int GetHashCode() => ID.GetHashCode();


        static IdentityInfo()
        {
            var sqlMap = ServerData.DataProviders.IdentitySQLMap;
            var playerSQLMap = ServerData.DataProviders.PlayerSQLMap;

            SQL_INSERT = 
            @$"{playerSQLMap.UUID},
            {sqlMap.IsTrue},
            {sqlMap.IsActive},
            {sqlMap.FirstName},
            {sqlMap.LastName},
            {sqlMap.Age},
            {sqlMap.Gender},
            {sqlMap.Description},
            {sqlMap.Portrait}";

            SQL_SELECT =
            @$"{sqlMap.ID},
            {sqlMap.IsTrue},
            {sqlMap.IsActive},
            {sqlMap.FirstName},
            {sqlMap.LastName},
            {sqlMap.Age},
            {sqlMap.Gender},
            {sqlMap.Description},
            {sqlMap.Portrait}";

            SQL_UPDATE =
            @$"{sqlMap.FirstName},
            {sqlMap.LastName},
            {sqlMap.Age},
            {sqlMap.Gender},
            {sqlMap.Description},
            {sqlMap.Portrait}";
        }
        public static readonly string SQL_INSERT;
        public static readonly string SQL_SELECT;

        public static readonly string SQL_UPDATE;

        internal static IdentityInfo FromSqlRowData(MySQLClient.ISqlRowData row, out bool isTrue, out bool isActive)
        {
            isTrue = row.Get<bool>(1);
            isActive = row.Get<bool>(2);
            var info = new IdentityInfo(
                id: row.Get<int>(0),
                identity: new(
                    firstName: row.Get<string>(3) ?? string.Empty,
                    lastname: row.Get<string>(4) ?? string.Empty,
                    age: row.Get<int>(5),
                    gender: (Gender)(row.Get<bool>(6) ? 1 : 0),
                    description: row.Get<string>(7) ?? string.Empty,
                    portrait: row.Get<string>(8)
                )
            );

            return info;
        }

        internal static object[] GetInsertIdentityQuery(Guid pcGuid, Identity identity, bool isTrue) => new object[]
        {
            pcGuid.ToUUIDString(),
            isTrue ? 1 : 0, 0,
            identity.FirstName,
            identity.LastName,
            identity.Age, identity.Gender == Gender.Male ? 0 : 1,
            identity.Description,
            identity.Portrait
        };

        internal object[] GetUpdateIdentityQuery() => new object[]
        {
            Identity.FirstName,
            Identity.LastName,
            Identity.Age,
            Identity.Gender == Gender.Male ? 0 : 1,
            Identity.Description,
            Identity.Portrait };
    }
}