namespace UserManagement.Models.Claims
{
    public class ClaimModel
    {
        public class Application
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public Role Role { get; set; }
        }

        public class Role
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public List<string> Permissions { get; set; }
            public Level Level { get; set; }
        }

        public class Level
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Scope { get; set; }
        }
    }
}
