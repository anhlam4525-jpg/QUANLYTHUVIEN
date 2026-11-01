using System.Text.RegularExpressions;

namespace LibraryManagement.BLL.Validators
{
    public class ReaderValidator
    {
        public bool IsValidReaderCode(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            code = code.Trim();
            return Regex.IsMatch(code, @"^[a-zA-Z0-9]{3,20}$");
        }

        public bool IsValidFullName(string? name)
        {
            return !string.IsNullOrWhiteSpace(name) && name.Trim().Length >= 2 && name.Trim().Length <= 100;
        }

        public bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            email = email.Trim();
            // Định dạng email chuẩn: name@domain.com / sv.edu.vn
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        public bool IsValidPhoneNumber(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return true; // Phone là optional
            phone = phone.Trim();
            return Regex.IsMatch(phone, @"^[0-9]{10,11}$");
        }
    }
}
