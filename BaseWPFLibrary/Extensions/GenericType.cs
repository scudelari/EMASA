using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace BaseWPFLibrary.Extensions
{
    public class GenericType : MarkupExtension
    {
        public GenericType() { }

        public GenericType(Type baseType, params Type[] innerTypes)
        {
            BaseType = baseType;
            InnerTypes = innerTypes;
        }

        public Type BaseType { get; set; }

        public Type[] InnerTypes { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            Type result = BaseType.MakeGenericType(InnerTypes);
            return result;
        }
    }
}
