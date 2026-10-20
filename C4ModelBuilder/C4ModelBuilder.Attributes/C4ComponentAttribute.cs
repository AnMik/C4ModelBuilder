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
    /// [C4Component("Главная страница", isRoot: true, technology: "ASP.NET Core")]
    /// public sealed class HomeController
    /// {
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method)]
    public sealed class C4ComponentAttribute : Attribute
    {
        public C4ComponentAttribute(string description, bool isRoot = false, string? technology = null)
        {
            Description = description;
            IsRoot = isRoot;
            Technology = technology;
        }

        /// <summary>
        /// Описание компонента, отображаемое на диаграмме. Обязательный параметр конструктора.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Признак корневого компонента — точки входа анализа. По умолчанию <c>false</c>.
        /// </summary>
        public bool IsRoot { get; }

        /// <summary>
        /// Технология реализации компонента (например, <c>ASP.NET Core</c>), отображаемая на диаграмме
        /// отдельной строкой. Необязательный параметр конструктора. По умолчанию не задана.
        /// </summary>
        public string? Technology { get; }
    }
}
