using System.Text.RegularExpressions;

namespace LibraryManagement.BLL.Validators
{
    public class ReaderValidator
    {
        public bool IsValidReaderCode(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            return Regex.IsMatch(code.Trim(), @"^[a-zA-Z0-9]{3,20}$");
        }

        public bool IsValidFullName(string? name)
        {
            return !string.IsNullOrWhiteSpace(name) && name.Trim().Length >= 2 && name.Trim().Length <= 100;
        }

        public bool IsValidClass(string? className)
        {
            if (string.IsNullOrWhiteSpace(className)) return false;
            return Regex.IsMatch(className.Trim(), @"^[a-zA-Z0-9_\-]{2,20}$");
        }

        public bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            // name@domain.tld — phần domain không được bắt đầu bằng dấu chấm
            return Regex.IsMatch(email.Trim(), @"^[^@\s]+@[^@\s\.][^@\s]*\.[^@\s\.]{2,}$");
        }

        /// <summary>Số điện thoại không bắt buộc; nếu nhập thì phải gồm 10–11 chữ số, bắt đầu bằng 0.</summary>
        public bool IsValidPhoneNumber(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return true;
            return Regex.IsMatch(phone.Trim(), @"^0[0-9]{9,10}$");
        }
    }
}
