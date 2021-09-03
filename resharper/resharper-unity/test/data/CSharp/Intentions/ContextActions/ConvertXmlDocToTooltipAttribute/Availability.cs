using UnityEngine;

public class MyBehaviour : MonoBehaviour
{
    public string WithoutXml{off}Doc;

    /// <summ{on}ary>
    /// Copy {on}this to tooltip
    /// </summary>
    public string WithXml{on}Doc;

    /// <summa{off}ry>
    /// Copy this to tooltip
    /// </summary>
    [Tooltip("Got a tooltip already")]
    public string WithTooltip{off}Already;

    /// <{off}summary>
    /// Copy this to {off}tooltip
    /// </summary>
    private string NotSerialised{off}Field;

    /// <summ{on}ary>
    /// Copy {on}this to tooltip
    /// </summary>
    public string WithXml{on}Doc2, WithXml{on}Doc3, WithXml{on}Doc4;
}

public class NotMonoBehaviour
{
    /// <summary>
    /// Copy {off}this to tooltip
    /// </summary>
    public string WithXml{off}Doc;
}
