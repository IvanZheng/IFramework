using System;
using System.Collections.Generic;
using System.Text;

namespace IFramework.Message
{
    public interface IMessageTypeProvider
    {
        IMessageTypeProvider Register(string code, Type messageType);
        IMessageTypeProvider Register(string code, string messageType);
        /// <summary>
        /// code mapping dictionary, key is code, value is message's type 
        /// </summary>
        /// <param name="codeMapping"></param>
        /// <returns></returns>
        IMessageTypeProvider Register(IDictionary<string, Type> codeMapping);
        /// <summary>
        /// code mapping dictionary, key is code, value is the fullNameWithAssembly of message's type 
        /// </summary>
        /// <param name="codeMapping"></param>
        /// <returns></returns>
        IMessageTypeProvider Register(IDictionary<string, string> codeMapping);
        string GetMessageCode(Type messageType);
        //string GetMessageCode(string messageType);
        Type GetMessageType(string code);
    }
}
