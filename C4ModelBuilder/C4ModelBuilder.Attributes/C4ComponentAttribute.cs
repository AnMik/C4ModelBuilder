#nullable enable

using System;

namespace C4ModelBuilder.Attributes
{
    /// <summary>
    /// Level 3
    /// </summary>
    /// <example>
    /// <code>
    /// [C4Component("Отправка промо-кампании")]
    /// public sealed class CampaignSender
    /// {
    /// }
    ///
    /// [C4Component("Главная страница", isRoot: true)]
    /// public sealed class HomeController
    /// {
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method)]
    public sealed class C4ComponentAttribute : Attribute
    {
        public C4ComponentAttribute(string description, bool isRoot = false)
        {
            Description = description;
            IsRoot = isRoot;
        }

        /// <summary>
        /// Описание компонента, отображаемое на диаграмме. Обязательный параметр конструктора.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Признак корневого компонента — точки входа анализа. По умолчанию <c>false</c>.
        /// </summary>
        public bool IsRoot { get; }
    }
}
