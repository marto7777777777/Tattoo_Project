namespace Tattoo_Project.DTOs.AuthDTOs
{
    public class VerifyRegisterCodeDto
    {
        public string Email { get; set; } = null!;

        public string Code { get; set; } = null!;
    }

    public class ResendRegisterCodeDto
    {
        public string Email { get; set; } = null!;
    }

    public class ForgotPasswordSendCodeDto
    {
        public string Email { get; set; } = null!;
    }

    public class VerifyPasswordResetCodeDto
    {
        public string Email { get; set; } = null!;

        public string Code { get; set; } = null!;
    }

    public class ResetPasswordWithCodeDto
    {
        public string Email { get; set; } = null!;

        public string Code { get; set; } = null!;

        public string NewPassword { get; set; } = null!;

        public string ConfirmNewPassword { get; set; } = null!;
    }

    public class ChangePasswordWithCodeDto
    {
        public string Code { get; set; } = null!;

        public string NewPassword { get; set; } = null!;

        public string ConfirmNewPassword { get; set; } = null!;
    }

    public class AuthUserDto
    {
        public string Id { get; set; } = null!;

        public string FirstName { get; set; } = null!;

        public string LastName { get; set; } = null!;

        public string UserName { get; set; } = null!;

        public string Email { get; set; } = null!;

        public IList<string> Roles { get; set; } = new List<string>();
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = null!;

        public AuthUserDto User { get; set; } = null!;
    }
}
