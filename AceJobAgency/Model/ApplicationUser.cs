using Microsoft.AspNetCore.Identity;

namespace AceJobAgency.Model
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Gender { get; set; }
        public string NRIC { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Resume { get; set; } // This will store the path or reference to the resume file
        public string ResumeOriginalName { get; set; } // Add this property
        public string WhoAmI { get; set; }
        public string CurrentSessionId { get; set; }

        // Encrypt the NRIC value
        private string EncryptNRIC(string nric)
        {
            if (string.IsNullOrEmpty(nric))
            {
                return nric;
            }

            // Placeholder for encryption logic
            byte[] nricBytes = System.Text.Encoding.UTF8.GetBytes(nric);
            string encryptedNRIC = Convert.ToBase64String(nricBytes);
            return encryptedNRIC;
        }

        // Decrypt the NRIC value
        private string DecryptNRIC(string encryptedNRIC)
        {
            if (string.IsNullOrEmpty(encryptedNRIC))
            {
                return encryptedNRIC;
            }

            // Placeholder for decryption logic
            byte[] nricBytes = Convert.FromBase64String(encryptedNRIC);
            string decryptedNRIC = System.Text.Encoding.UTF8.GetString(nricBytes);
            return decryptedNRIC;
        }
    }
}
