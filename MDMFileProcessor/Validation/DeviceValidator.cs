using FluentValidation;
using ChunkProcessing.Models;

namespace ChunkProcessing.Validation
{
    public class DeviceValidator : AbstractValidator<Device>
    {
        public DeviceValidator()
        {
            RuleFor(x => x.DeviceId).GreaterThan(0);
            RuleFor(x => x.DeviceName).NotEmpty().Length(2, 100);
            RuleFor(x => x.SerialNumber).NotEmpty().Length(2, 100);
            RuleFor(x => x.OSVersion).NotEmpty().MaximumLength(50);
            RuleFor(x => x.ComplianceStatus).NotEmpty().MaximumLength(50);
            RuleFor(x => x.LastCheckIn).NotEmpty().LessThanOrEqualTo(DateTime.UtcNow);
            RuleFor(x => x.UserPrincipalName).EmailAddress().When(x => !string.IsNullOrEmpty(x.UserPrincipalName));
        }
    }
}
