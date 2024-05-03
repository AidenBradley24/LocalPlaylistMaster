using System.ComponentModel;
using System.Windows.Markup;

namespace LocalPlaylistMaster.Extensions
{
    public class EnumExtension : MarkupExtension
    {
        private readonly Type myType;

        public EnumExtension(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);
            myType = type;
            if (!myType.IsEnum)
                throw new ArgumentException("Type must be an Enum.");
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var enumValues = Enum.GetValues(myType);

            return (
                from object enumValue in enumValues
                select new EnumerationMember
                {
                    Value = enumValue,
                    Description = GetDescription(enumValue)
                }).ToArray();
        }

        private string? GetDescription(object enumValue) => myType
              .GetField(enumValue.ToString() ?? "")?
              .GetCustomAttributes(typeof(DescriptionAttribute), false)
              .FirstOrDefault() is DescriptionAttribute descriptionAttribute
              ? descriptionAttribute.Description
              : enumValue.ToString();

        public class EnumerationMember
        {
            public string? Description { get; set; }
            public object? Value { get; set; }
        }
    }
}
