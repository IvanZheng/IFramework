using System.ComponentModel;

namespace IFramework.Exceptions
{
    public enum ErrorCode
    {
        /// <summary>
        /// 成功
        /// </summary>
        [Description("成功")]
        NoError = 0,
        /// <summary>
        /// 重复对象
        /// </summary>
        [Description("重复对象")]
        DuplicatedObject = 0x7ffffffc,
        /// <summary>
        /// http错误
        /// </summary>
        [Description("http错误")]
        HttpStatusError = 0x7ffffffd,
        /// <summary>
        /// 无效参数
        /// </summary>
        [Description("无效参数")]
        InvalidParameters = 0x7ffffffe,
        /// <summary>
        /// 系统错误
        /// </summary>
        [Description("系统错误")]
        UnknownError = 0x7fffffff
    }
}