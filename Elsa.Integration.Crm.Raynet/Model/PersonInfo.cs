
namespace Elsa.Integration.Crm.Raynet.Model
{
    public class PersonInfo
    {
        public long Id { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string Email => ContactInfo?.Email;

        public ContactInfo ContactInfo { get; set; }

        public override string ToString()
        {
            return $"{Id}: {FirstName} {LastName} {ContactInfo?.Email}";
        }
    }
}
