namespace Elsa.Users.ViewModel
{
    public class UserViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public bool IsActive { get; set; }
        public bool IsLocked { get; set; }

        public bool IsOnline { get; set; }
    }
}
