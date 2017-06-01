using System;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Principal;
using System.Threading;
using System.Xml;

namespace Microsoft.KafkaNET.Library.Util
{
    /// <summary>
    ///     Represents an exception formatter that formats exception objects as XML.
    /// </summary>
    public sealed class XmlExceptionFormatter : ExceptionFormatter
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="XmlExceptionFormatter" /> class using the specified
        ///     <see cref="XmlWriter" /> and <see cref="Exception" /> objects.
        /// </summary>
        /// <param name="xmlWriter">The <see cref="XmlWriter" /> in which to write the XML.</param>
        /// <param name="exception">The <see cref="Exception" /> to format.</param>
        /// <param name="handlingInstanceId">The id of the handling chain.</param>
        public XmlExceptionFormatter(XmlWriter xmlWriter, Exception exception, Guid handlingInstanceId)
            : base(exception, handlingInstanceId)
        {
            if (xmlWriter == null)
            {
                throw new ArgumentNullException("xmlWriter");
            }

            Writer = xmlWriter;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="XmlExceptionFormatter" /> class using the specified
        ///     <see cref="TextWriter" /> and <see cref="Exception" /> objects.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter" /> in which to write the XML.</param>
        /// <param name="exception">The <see cref="Exception" /> to format.</param>
        /// <remarks>
        ///     An <see cref="XmlTextWriter" /> with indented formatting is created from the  specified <see cref="TextWriter" />.
        /// </remarks>
        /// <param name="handlingInstanceId">The id of the handling chain.</param>
        public XmlExceptionFormatter(TextWriter writer, Exception exception, Guid handlingInstanceId)
            : base(exception, handlingInstanceId)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            var textWriter = new XmlTextWriter(writer);
            textWriter.Formatting = Formatting.Indented;
            Writer = textWriter;
        }

        /// <summary>
        ///     Gets the underlying <see cref="XmlWriter" /> that the formatted exception is written to.
        /// </summary>
        /// <value>
        ///     The underlying <see cref="XmlWriter" /> that the formatted exception is written to.
        /// </value>
        public XmlWriter Writer { get; }

        /// <summary>
        ///     Formats the <see cref="Exception" /> into the underlying stream.
        /// </summary>
        public override void Format()
        {
            Writer.WriteStartElement("Exception");
            if (HandlingInstanceId != Guid.Empty)
            {
                Writer.WriteAttributeString(
                                            "handlingInstanceId",
                                            HandlingInstanceId.ToString("D", CultureInfo.InvariantCulture));
            }

            base.Format();

            Writer.WriteEndElement();
        }

        /// <summary>
        ///     Writes the current date and time to the <see cref="XmlWriter" />.
        /// </summary>
        /// <param name="datetime">The current time.</param>
        protected override void WriteDateTime(DateTime datetime)
        {
            WriteSingleElement("DateTime",
                               datetime.ToUniversalTime().ToString("u", DateTimeFormatInfo.InvariantInfo));
        }

        /// <summary>
        ///     Writes the value of the <see cref="Exception.Message" /> property to the <see cref="XmlWriter" />.
        /// </summary>
        /// <param name="message">The message to write.</param>
        protected override void WriteMessage(string message)
        {
            WriteSingleElement("Message", message);
        }

        /// <summary>
        ///     Writes a generic description to the <see cref="XmlWriter" />.
        /// </summary>
        protected override void WriteDescription()
        {
            WriteSingleElement("Description",
                               string.Format(CultureInfo.CurrentCulture, ExceptionWasCaught, Exception.GetType().FullName));
        }

        /// <summary>
        ///     Writes the value of the specified help link taken
        ///     from the value of the <see cref="Exception.HelpLink" />
        ///     property to the <see cref="XmlWriter" />.
        /// </summary>
        /// <param name="helpLink">The exception's help link.</param>
        protected override void WriteHelpLink(string helpLink)
        {
            WriteSingleElement("HelpLink", helpLink);
        }

        /// <summary>
        ///     Writes the value of the specified stack trace taken from the value of the <see cref="Exception.StackTrace" />
        ///     property to the <see cref="XmlWriter" />.
        /// </summary>
        /// <param name="stackTrace">The stack trace of the exception.</param>
        protected override void WriteStackTrace(string stackTrace)
        {
            WriteSingleElement("StackTrace", stackTrace);
        }

        /// <summary>
        ///     Writes the value of the specified source taken from the value of the <see cref="Exception.Source" /> property to
        ///     the <see cref="XmlWriter" />.
        /// </summary>
        /// <param name="source">The source of the exception.</param>
        protected override void WriteSource(string source)
        {
            WriteSingleElement("Source", source);
        }

        /// <summary>
        ///     Writes the value of the <see cref="Type.AssemblyQualifiedName" />
        ///     property for the specified exception type to the <see cref="XmlWriter" />.
        /// </summary>
        /// <param name="exceptionType">The <see cref="Type" /> of the exception.</param>
        protected override void WriteExceptionType(Type exceptionType)
        {
            if (exceptionType == null)
            {
                throw new ArgumentException("Exception type cannot be null");
            }

            WriteSingleElement("ExceptionType", exceptionType.AssemblyQualifiedName);
        }

        /// <summary>
        ///     Writes and formats the exception and all nested inner exceptions to the <see cref="XmlWriter" />.
        /// </summary>
        /// <param name="exceptionToFormat">The exception to format.</param>
        /// <param name="outerException">The outer exception. This value will be null when writing the outer-most exception.</param>
        protected override void WriteException(Exception exceptionToFormat, Exception outerException)
        {
            if (outerException != null)
            {
                Writer.WriteStartElement("InnerException");

                base.WriteException(exceptionToFormat, outerException);

                Writer.WriteEndElement();
            }
            else
            {
                base.WriteException(exceptionToFormat, outerException);
            }
        }

        /// <summary>
        ///     Writes the name and value of the specified property to the <see cref="XmlWriter" />.
        /// </summary>
        /// <param name="propertyInfo">The reflected <see cref="PropertyInfo" /> object.</param>
        /// <param name="value">The value of the <see cref="PropertyInfo" /> object.</param>
        protected override void WritePropertyInfo(PropertyInfo propertyInfo, object value)
        {
            if (propertyInfo == null)
            {
                throw new ArgumentException("Property info cannot be null");
            }

            var propertyValueString = UndefinedValue;

            if (value != null)
            {
                propertyValueString = value.ToString();
            }

            Writer.WriteStartElement("Property");
            Writer.WriteAttributeString("name", propertyInfo.Name);
            Writer.WriteString(propertyValueString);
            Writer.WriteEndElement();
        }

        /// <summary>
        ///     Writes the name and value of the <see cref="FieldInfo" /> object to the <see cref="XmlWriter" />.
        /// </summary>
        /// <param name="fieldInfo">The reflected <see cref="FieldInfo" /> object.</param>
        /// <param name="value">The value of the <see cref="FieldInfo" /> object.</param>
        protected override void WriteFieldInfo(FieldInfo fieldInfo, object value)
        {
            if (fieldInfo == null)
            {
                throw new ArgumentException("Field info cannot be null");
            }

            if (value == null)
            {
                throw new ArgumentException("Value cannot be null");
            }

            var fieldValueString = UndefinedValue;

            if (fieldValueString != null)
            {
                fieldValueString = value.ToString();
            }

            Writer.WriteStartElement("Field");
            Writer.WriteAttributeString("name", fieldInfo.Name);
            Writer.WriteString(value.ToString());
            Writer.WriteEndElement();
        }

        /// <summary>
        ///     Writes additional information to the <see cref="XmlWriter" />.
        /// </summary>
        /// <param name="additionalInformation">Additional information to be included with the exception report</param>
        protected override void WriteAdditionalInfo(NameValueCollection additionalInformation)
        {
            if (additionalInformation == null)
            {
                throw new ArgumentException("Additional information cannot be null");
            }

            Writer.WriteStartElement("additionalInfo");

            foreach (var name in additionalInformation.AllKeys)
            {
                Writer.WriteStartElement("info");
                Writer.WriteAttributeString("name", name);
                Writer.WriteAttributeString("value", additionalInformation[name]);
                Writer.WriteEndElement();
            }

            Writer.WriteEndElement();
        }

        private void WriteSingleElement(string elementName, string elementText)
        {
            Writer.WriteStartElement(elementName);
            Writer.WriteString(elementText);
            Writer.WriteEndElement();
        }
    }

    /// <summary>
    ///     Represents an exception formatter that formats exception objects as text.
    /// </summary>
    public sealed class TextExceptionFormatter : ExceptionFormatter
    {
        private int innerDepth;

        /// <summary>
        ///     Initializes a new instance of the
        ///     <see cref="TextExceptionFormatter" /> using the specified
        ///     <see cref="TextWriter" /> and <see cref="Exception" />
        ///     objects.
        /// </summary>
        /// <param name="writer">The stream to write formatting information to.</param>
        /// <param name="exception">The exception to format.</param>
        public TextExceptionFormatter(TextWriter writer, Exception exception)
            : this(writer, exception, Guid.Empty) { }

        /// <summary>
        ///     Initializes a new instance of the
        ///     <see cref="TextExceptionFormatter" /> using the specified
        ///     <see cref="TextWriter" /> and <see cref="Exception" />
        ///     objects.
        /// </summary>
        /// <param name="writer">The stream to write formatting information to.</param>
        /// <param name="exception">The exception to format.</param>
        /// <param name="handlingInstanceId">The id of the handling chain.</param>
        public TextExceptionFormatter(TextWriter writer, Exception exception, Guid handlingInstanceId)
            : base(exception, handlingInstanceId)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            Writer = writer;
        }

        /// <summary>
        ///     Gets the underlying <see cref="TextWriter" />
        ///     that the current formatter is writing to.
        /// </summary>
        public TextWriter Writer { get; }

        /// <summary>
        ///     Formats the <see cref="Exception" /> into the underlying stream.
        /// </summary>
        public override void Format()
        {
            if (HandlingInstanceId != Guid.Empty)
            {
                Writer.WriteLine(
                                 "HandlingInstanceID: {0}",
                                 HandlingInstanceId.ToString("D", CultureInfo.InvariantCulture));
            }
            base.Format();
        }

        /// <summary>
        ///     Writes a generic description to the underlying text stream.
        /// </summary>
        protected override void WriteDescription()
        {
            // An exception of type {0} occurred and was caught.
            // -------------------------------------------------
            // Workaround for TFS Bug # 752845, refer bug description for more information
            var line = string.Format(CultureInfo.CurrentCulture, "An exception of type '{0}' occurred and was caught.",
                                     Exception.GetType().FullName);
            Writer.WriteLine(line);

            var separator = new string('-', line.Length);

            Writer.WriteLine(separator);
        }

        /// <summary>
        ///     Writes and formats the exception and all nested inner exceptions to the <see cref="TextWriter" />.
        /// </summary>
        /// <param name="exceptionToFormat">The exception to format.</param>
        /// <param name="outerException">
        ///     The outer exception. This
        ///     value will be null when writing the outer-most exception.
        /// </param>
        protected override void WriteException(Exception exceptionToFormat, Exception outerException)
        {
            if (outerException != null)
            {
                innerDepth++;
                Indent();
                var temp = InnerException;
                var separator = new string('-', temp.Length);
                Writer.WriteLine(temp);
                Indent();
                Writer.WriteLine(separator);

                base.WriteException(exceptionToFormat, outerException);
                innerDepth--;
            }
            else
            {
                base.WriteException(exceptionToFormat, outerException);
            }

            if (FlattenAggregateException && exceptionToFormat is AggregateException)
            {
                var exceptions = ((AggregateException) exceptionToFormat).InnerExceptions;
                Writer.WriteLine("--output InnerExceptions of AggregateException, total count:{0}", exceptions.Count);
                var idx = 0;
                foreach (var e in exceptions)
                {
                    idx++;
                    Writer.WriteLine("-- {0} of {1} of InnerExceptions:", idx, exceptions.Count);
                    base.WriteException(e, null);
                }
            }
        }

        /// <summary>
        ///     Writes the current date and time to the <see cref="TextWriter" />.
        /// </summary>
        /// <param name="datetime">The current time.</param>
        protected override void WriteDateTime(DateTime datetime)
        {
            Writer.WriteLine(datetime.ToUniversalTime().ToString("G", DateTimeFormatInfo.InvariantInfo));
        }

        /// <summary>
        ///     Writes the value of the <see cref="Type.AssemblyQualifiedName" />
        ///     property for the specified exception type to the <see cref="TextWriter" />.
        /// </summary>
        /// <param name="exceptionType">The <see cref="Type" /> of the exception.</param>
        protected override void WriteExceptionType(Type exceptionType)
        {
            if (exceptionType == null)
            {
                throw new ArgumentException("Exception type cannot be null");
            }

            IndentAndWriteLine(TypeString, exceptionType.AssemblyQualifiedName);
        }

        /// <summary>
        ///     Writes the value of the <see cref="Exception.Message" />
        ///     property to the underyling <see cref="TextWriter" />.
        /// </summary>
        /// <param name="message">The message to write.</param>
        protected override void WriteMessage(string message)
        {
            IndentAndWriteLine(Message, message);
        }

        /// <summary>
        ///     Writes the value of the specified source taken
        ///     from the value of the <see cref="Exception.Source" />
        ///     property to the <see cref="TextWriter" />.
        /// </summary>
        /// <param name="source">The source of the exception.</param>
        protected override void WriteSource(string source)
        {
            IndentAndWriteLine(Source, source);
        }

        /// <summary>
        ///     Writes the value of the specified help link taken
        ///     from the value of the <see cref="Exception.HelpLink" />
        ///     property to the <see cref="TextWriter" />.
        /// </summary>
        /// <param name="helpLink">The exception's help link.</param>
        protected override void WriteHelpLink(string helpLink)
        {
            IndentAndWriteLine(HelpLink, helpLink);
        }

        /// <summary>
        ///     Writes the name and value of the specified property to the <see cref="TextWriter" />.
        /// </summary>
        /// <param name="propertyInfo">The reflected <see cref="PropertyInfo" /> object.</param>
        /// <param name="value">The value of the <see cref="PropertyInfo" /> object.</param>
        protected override void WritePropertyInfo(PropertyInfo propertyInfo, object value)
        {
            if (propertyInfo == null)
            {
                throw new ArgumentException("Property info cannot be null");
            }

            Indent();
            Writer.Write(propertyInfo.Name);
            Writer.Write(" : ");
            Writer.WriteLine(value);
        }

        /// <summary>
        ///     Writes the name and value of the specified field to the <see cref="TextWriter" />.
        /// </summary>
        /// <param name="fieldInfo">The reflected <see cref="FieldInfo" /> object.</param>
        /// <param name="value">The value of the <see cref="FieldInfo" /> object.</param>
        protected override void WriteFieldInfo(FieldInfo fieldInfo, object value)
        {
            if (fieldInfo == null)
            {
                throw new ArgumentException("Field info cannot be null");
            }

            Indent();
            Writer.Write(fieldInfo.Name);
            Writer.Write(" : ");
            Writer.WriteLine(value);
        }

        /// <summary>
        ///     Writes the value of the <see cref="System.Exception.StackTrace" /> property to the <see cref="TextWriter" />.
        /// </summary>
        /// <param name="stackTrace">The stack trace of the exception.</param>
        /// <remarks>
        ///     If there is no stack trace available, an appropriate message will be displayed.
        /// </remarks>
        protected override void WriteStackTrace(string stackTrace)
        {
            Indent();
            Writer.Write(StackTrace);
            Writer.Write(" : ");
            if (stackTrace == null || stackTrace.Length == 0)
            {
                Writer.WriteLine(StackTraceUnavailable);
            }
            else
            {
                // The stack trace has all '\n's prepended with a number
                // of tabs equal to the InnerDepth property in order
                // to make the formatting pretty.
                var indentation = new string('\t', innerDepth);
                var indentedStackTrace = stackTrace.Replace("\n", "\n" + indentation);

                Writer.WriteLine(indentedStackTrace);
                Writer.WriteLine();
            }
        }

        /// <summary>
        ///     Writes the additional properties to the <see cref="TextWriter" />.
        /// </summary>
        /// <param name="additionalInformation">Additional information to be included with the exception report</param>
        protected override void WriteAdditionalInfo(NameValueCollection additionalInformation)
        {
            if (additionalInformation == null)
            {
                throw new ArgumentException("Additional information cannot be null");
            }

            Writer.WriteLine(AdditionalInfoConst);
            Writer.WriteLine();

            foreach (var name in additionalInformation.AllKeys)
            {
                Writer.Write(name);
                Writer.Write(" : ");
                Writer.Write(additionalInformation[name]);
                Writer.Write("\n");
            }
        }

        /// <summary>
        ///     Indents the <see cref="TextWriter" />.
        /// </summary>
        private void Indent()
        {
            for (var i = 0; i < innerDepth; i++)
            {
                Writer.Write("\t");
            }
        }

        private void IndentAndWriteLine(string format, params object[] arg)
        {
            Indent();
            Writer.WriteLine(format, arg);
        }
    }

    /// <summary>
    ///     Represents the base class from which all implementations of exception formatters must derive. The formatter
    ///     provides functionality for formatting <see cref="Exception" /> objects.
    /// </summary>
    public abstract class ExceptionFormatter
    {
        // <autogenerated />
        public const string AdditionalInfoConst = "Additional Info:";

        public const string ExceptionWasCaught = "An exception of type '{0}' occurred and was caught.";
        public const string FieldAccessFailed = "Access failed";
        public const string HelpLink = "Help link : {0}";
        public const string InnerException = "Inner Exception";
        public const string Message = "Message : {0}";
        public const string PermissionDenied = "Permission Denied";
        public const string PropertyAccessFailed = "Access failed";
        public const string Source = "Source : {0}";
        public const string StackTrace = "Stack Trace";
        public const string StackTraceUnavailable = "The stack trace is unavailable.";
        public const string TypeString = "Type : {0}";
        public const string UndefinedValue = "<undefined value>";

        private static readonly ArrayList IgnoredProperties = new ArrayList(
                                                                            new[] {"Source", "Message", "HelpLink", "InnerException", "StackTrace"});

        private NameValueCollection additionalInfo;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExceptionFormatter" /> class with an <see cref="Exception" /> to
        ///     format.
        /// </summary>
        /// <param name="exception">The <see cref="Exception" /> object to format.</param>
        /// <param name="handlingInstanceId">The id of the handling chain.</param>
        protected ExceptionFormatter(Exception exception, Guid handlingInstanceId)
        {
            if (exception == null)
            {
                throw new ArgumentNullException("exception");
            }

            Exception = exception;
            HandlingInstanceId = handlingInstanceId;
        }

        /// <summary>
        ///     Gets the <see cref="Exception" /> to format.
        /// </summary>
        /// <value>
        ///     The <see cref="Exception" /> to format.
        /// </value>
        public Exception Exception { get; }

        /// <summary>
        ///     Gets the id of the handling chain requesting a formatting.
        /// </summary>
        /// <value>
        ///     The id of the handling chain requesting a formatting, or <see cref="Guid.Empty" /> if no such id is available.
        /// </value>
        public Guid HandlingInstanceId { get; }

        /// <summary>
        ///     Gets additional information related to the <see cref="Exception" /> but not
        ///     stored in the exception (eg: the time in which the <see cref="Exception" /> was
        ///     thrown).
        /// </summary>
        /// <value>
        ///     Additional information related to the <see cref="Exception" /> but not
        ///     stored in the exception (for example, the time when the <see cref="Exception" /> was
        ///     thrown).
        /// </value>
        public NameValueCollection AdditionalInfo
        {
            get
            {
                if (additionalInfo == null)
                {
                    additionalInfo = new NameValueCollection();
                    additionalInfo.Add("MachineName", GetMachineName());
                    additionalInfo.Add("TimeStamp", DateTime.UtcNow.ToString(CultureInfo.CurrentCulture));
                    additionalInfo.Add("FullName", Assembly.GetExecutingAssembly().FullName);
                    additionalInfo.Add("AppDomainName", AppDomain.CurrentDomain.FriendlyName);
                    additionalInfo.Add("ThreadIdentity", Thread.CurrentPrincipal.Identity.Name);
                    additionalInfo.Add("WindowsIdentity", GetWindowsIdentity());
                }

                return additionalInfo;
            }
        }

        public bool FlattenAggregateException { get; set; }

        /// <summary>
        ///     Formats the <see cref="Exception" /> into the underlying stream.
        /// </summary>
        public virtual void Format()
        {
            WriteDescription();
            WriteDateTime(DateTime.UtcNow);
            WriteException(Exception, null);
        }

        /// <summary>
        ///     Formats the exception and all nested inner exceptions.
        /// </summary>
        /// <param name="exceptionToFormat">The exception to format.</param>
        /// <param name="outerException">
        ///     The outer exception. This
        ///     value will be null when writing the outer-most exception.
        /// </param>
        /// <remarks>
        ///     <para>
        ///         This method calls itself recursively until it reaches
        ///         an exception that does not have an inner exception.
        ///     </para>
        ///     <para>
        ///         This is a template method which calls the following
        ///         methods in order
        ///         <list type="number">
        ///             <item>
        ///                 <description>
        ///                     <see cref="WriteExceptionType" />
        ///                 </description>
        ///             </item>
        ///             <item>
        ///                 <description>
        ///                     <see cref="WriteMessage" />
        ///                 </description>
        ///             </item>
        ///             <item>
        ///                 <description>
        ///                     <see cref="WriteSource" />
        ///                 </description>
        ///             </item>
        ///             <item>
        ///                 <description>
        ///                     <see cref="WriteHelpLink" />
        ///                 </description>
        ///             </item>
        ///             <item>
        ///                 <description>
        ///                     <see cref="WriteReflectionInfo" />
        ///                 </description>
        ///             </item>
        ///             <item>
        ///                 <description>
        ///                     <see cref="WriteStackTrace" />
        ///                 </description>
        ///             </item>
        ///             <item>
        ///                 <description>
        ///                     If the specified exception has an inner exception
        ///                     then it makes a recursive call. <see cref="WriteException" />
        ///                 </description>
        ///             </item>
        ///         </list>
        ///     </para>
        /// </remarks>
        protected virtual void WriteException(Exception exceptionToFormat, Exception outerException)
        {
            if (exceptionToFormat == null)
            {
                throw new ArgumentNullException("exceptionToFormat");
            }

            WriteExceptionType(exceptionToFormat.GetType());
            WriteMessage(exceptionToFormat.Message);
            WriteSource(exceptionToFormat.Source);
            WriteHelpLink(exceptionToFormat.HelpLink);
            WriteReflectionInfo(exceptionToFormat);
            WriteStackTrace(exceptionToFormat.StackTrace);

            // We only want additional information on the top most exception
            if (outerException == null)
            {
                WriteAdditionalInfo(AdditionalInfo);
            }

            var inner = exceptionToFormat.InnerException;

            if (inner != null)
            {
                WriteException(inner, exceptionToFormat);
            }
        }

        /// <summary>
        ///     Formats an <see cref="Exception" /> using reflection to get the information.
        /// </summary>
        /// <param name="exceptionToFormat">
        ///     The <see cref="Exception" /> to be formatted.
        /// </param>
        /// <remarks>
        ///     <para>
        ///         This method reflects over the public, instance properties
        ///         and public, instance fields
        ///         of the specified exception and prints them to the formatter.
        ///         Certain property names are ignored
        ///         because they are handled explicitly in other places.
        ///     </para>
        /// </remarks>
        protected void WriteReflectionInfo(Exception exceptionToFormat)
        {
            if (exceptionToFormat == null)
            {
                throw new ArgumentNullException("exceptionToFormat");
            }

            var type = exceptionToFormat.GetType();
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            object value;

            foreach (var property in properties)
            {
                if (property.CanRead && IgnoredProperties.IndexOf(property.Name) == -1 &&
                    property.GetIndexParameters().Length == 0)
                {
                    try
                    {
                        value = property.GetValue(exceptionToFormat, null);
                    }
                    catch (TargetInvocationException)
                    {
                        value = PropertyAccessFailed;
                    }
                    WritePropertyInfo(property, value);
                }
            }

            foreach (var field in fields)
            {
                try
                {
                    value = field.GetValue(exceptionToFormat);
                }
                catch (TargetInvocationException)
                {
                    value = FieldAccessFailed;
                }
                WriteFieldInfo(field, value);
            }
        }

        /// <summary>
        ///     When overridden by a class, writes a description of the caught exception.
        /// </summary>
        protected abstract void WriteDescription();

        /// <summary>
        ///     When overridden by a class, writes the current time.
        /// </summary>
        /// <param name="dateTime">The current time.</param>
        protected abstract void WriteDateTime(DateTime dateTime);

        /// <summary>
        ///     When overridden by a class, writes the <see cref="Type" /> of the current exception.
        /// </summary>
        /// <param name="exceptionType">The <see cref="Type" /> of the exception.</param>
        protected abstract void WriteExceptionType(Type exceptionType);

        /// <summary>
        ///     When overridden by a class, writes the <see cref="System.Exception.Message" />.
        /// </summary>
        /// <param name="message">The message to write.</param>
        protected abstract void WriteMessage(string message);

        /// <summary>
        ///     When overridden by a class, writes the value of the <see cref="System.Exception.Source" /> property.
        /// </summary>
        /// <param name="source">The source of the exception.</param>
        protected abstract void WriteSource(string source);

        /// <summary>
        ///     When overridden by a class, writes the value of the <see cref="System.Exception.HelpLink" /> property.
        /// </summary>
        /// <param name="helpLink">The help link for the exception.</param>
        protected abstract void WriteHelpLink(string helpLink);

        /// <summary>
        ///     When overridden by a class, writes the value of the <see cref="System.Exception.StackTrace" /> property.
        /// </summary>
        /// <param name="stackTrace">The stack trace of the exception.</param>
        protected abstract void WriteStackTrace(string stackTrace);

        /// <summary>
        ///     When overridden by a class, writes the value of a <see cref="PropertyInfo" /> object.
        /// </summary>
        /// <param name="propertyInfo">The reflected <see cref="PropertyInfo" /> object.</param>
        /// <param name="value">The value of the <see cref="PropertyInfo" /> object.</param>
        protected abstract void WritePropertyInfo(PropertyInfo propertyInfo, object value);

        /// <summary>
        ///     When overridden by a class, writes the value of a <see cref="FieldInfo" /> object.
        /// </summary>
        /// <param name="fieldInfo">The reflected <see cref="FieldInfo" /> object.</param>
        /// <param name="value">The value of the <see cref="FieldInfo" /> object.</param>
        protected abstract void WriteFieldInfo(FieldInfo fieldInfo, object value);

        /// <summary>
        ///     When overridden by a class, writes additional properties if available.
        /// </summary>
        /// <param name="additionalInformation">Additional information to be included with the exception report</param>
        protected abstract void WriteAdditionalInfo(NameValueCollection additionalInformation);

        private static string GetMachineName()
        {
            var machineName = string.Empty;
            try
            {
                machineName = Environment.MachineName;
            }
            catch (SecurityException)
            {
                machineName = PermissionDenied;
            }

            return machineName;
        }

        private static string GetWindowsIdentity()
        {
            var windowsIdentity = string.Empty;
            try
            {
                windowsIdentity = WindowsIdentity.GetCurrent().Name;
            }
            catch (SecurityException)
            {
                windowsIdentity = PermissionDenied;
            }

            return windowsIdentity;
        }
    }
}