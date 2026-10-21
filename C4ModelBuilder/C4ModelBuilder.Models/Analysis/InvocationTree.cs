namespace C4ModelBuilder.Models.Analysis;

/// <summary>
/// Дерево вызовов для одного корневого компонента (результат анализа одного root-класса).
/// Служит промежуточным представлением между анализом кода и построением диаграммы.
/// </summary>
public sealed class InvocationTree(
    string nodeName,
    string? c4ComponentDescription,
    string? c4ComponentTechnology = null)
{
    /// <summary>
    /// Сигнатура узла (например, "AccountController" или "AdminController.SendPromoAsync").
    /// </summary>
    public string NodeName { get; } = nodeName;

    /// <summary>
    /// Данные атрибута <c>C4Component</c>, если он проставлен на узле.
    /// </summary>
    public string? C4ComponentDescription { get; } = c4ComponentDescription;

    /// <summary>
    /// Технология из атрибута <c>C4Component</c>, если она задана на узле. <c>null</c> — технология
    /// не указана; от класса к методу не наследуется.
    /// </summary>
    public string? C4ComponentTechnology { get; } = c4ComponentTechnology;

    /// <summary>
    /// Признак того, что узел создан для вызова метода своего класса: такой узел не становится
    /// концом связи, а связь устанавливается между вызывающим компонентом и вызванным методом.
    /// </summary>
    public bool IsSameClassCall { get; init; }

    /// <summary>
    /// Дочерние узлы — вызовы методов из текущего метода.
    /// </summary>
    public IReadOnlyList<InvocationTree> Invocations { get; } = new List<InvocationTree>();

    /// <summary>
    /// Добавить дочерний узел.
    /// </summary>
    public void AddInvocation(InvocationTree invocationTree) => ((List<InvocationTree>)Invocations).Add(invocationTree);
}
