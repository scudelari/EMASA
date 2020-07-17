using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using BaseWPFLibrary.Others;

namespace BaseWPFLibrary.ValidationRules
{
    [ContentProperty("Binding")]
    [MarkupExtensionReturnType(typeof(object))]
    public class BindValidation : MarkupExtension
    {
        public BindValidation()
        {

        }
        public BindValidation(Binding binding, string valRule = null)
        {
            Binding = binding;
            ValRule = valRule;
        }
        public BindValidation(Binding binding)
        {
            Binding = binding;
        }

        [ConstructorArgument("binding")]
        public Binding Binding { get; set; }

        [ConstructorArgument("valRule")]
        public string ValRule { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            // Tries to parse the validation rule's string - the separator is |
            if (!string.IsNullOrEmpty(ValRule))
            {
                Regex pattern = new Regex(@"(.*?/*)/{1}|(.*$)");
                foreach (Match m in pattern.Matches(ValRule))
                {
                    for (int i = 0; i < m.Groups.Count; i++)
                    {
                        string statement = m.Groups[i].Value;

                        if (string.IsNullOrEmpty(statement)) continue;

                        string[] parts = statement.Split(new char[] { '|' });
                        string fqn = $"{GetType().Namespace}.{parts[0]}";
                        object validator = null;

                        if (parts.Length == 1)
                            validator = EmasaWPFLibraryStaticMethods.FastActivator.CreateInstance(Type.GetType(fqn));
                        else
                            validator = EmasaWPFLibraryStaticMethods.FastActivator<string[]>.CreateInstance(Type.GetType(fqn), parts.Skip(1).ToArray());

                        Binding.ValidationRules.Add(validator as ValidationRule);
                    }
                }
            }

            Binding.NotifyOnValidationError = true;
            Binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;

            return Binding.ProvideValue(serviceProvider);
        }
    }
}
