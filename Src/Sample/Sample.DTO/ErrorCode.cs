using System.ComponentModel;

namespace Sample.DTO
{
    public enum ErrorCode
    {
        NoError,
        [Description("唯一约束检查冲突: {0}")]
        UniqueConstraint,
        [Description("数据约束检查冲突: {0}")]
        ConstraintCheckViolation,
        [Description("重复键值冲突: {0}")]
        UsernameAlreadyExists,
        [Description("username:{0} or password is wrong!")] WrongUsernameOrPassword,
        UserNotExists,
        CountNotEnough,
        [Description("Bank Account({0}) already exists!")]
        BankAccountAlreadyExists,
        CommandInvalid = 0x7ffffffe,
        UnknownError = 0x7fffffff,
    }
}