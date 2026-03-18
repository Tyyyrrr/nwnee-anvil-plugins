namespace ServerData.SQLSchema
{
    public interface IIdentitySQLMap : ISQLMap
    {
        public string ID {get;}
        public string IsActive {get;}
        public string IsTrue {get;}
        public string FirstName {get;}
        public string LastName {get;}
        public string Age {get;}
        public string Gender {get;}
        public string Description {get;}
        public string Portrait {get;}
    }
}