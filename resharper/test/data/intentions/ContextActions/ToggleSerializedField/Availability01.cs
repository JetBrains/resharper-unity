using System;
using UnityEngine;

public class Foo : MonoBehaviour
{
    public int my{on:Make:field:non-serialized}Value;
    private int my{on:To:serialized:field}Value2;
    [SerializeField] private int my{on:Make:field:non-serialized}Value3;
    [NonSerialized] public int my{on:To:serialized:field}Value4;
    // Valid. NonSerialized wins - this is not serialized
    [SerializeField] [NonSerialized] private int my{on:To:serialized:field}Value5;
    [SerializeField] [NonSerialized] public int my{on:To:serialized:field}Value6;

    public int my{on:Make:field:'myValue7':non-serialized}Value7, my{on:Make:field:'myValue8':non-serialized}Value8, my{on:Make:field:'myValue9':non-serialized}Value9;

    public int my{on:Make:all:fields:non-serialized}value10, my{on:Make:all:fields:non-serialized}Value10;

    private int my{on:Make:field:'myValue11':serialized}Value11, my{on:Make:field:'myValue12':serialized}Value12;

    private int my{on:Make:all:fields:serialized}Value13, my{on:Make:all:fields:serialized}Value14;

    public static int my{on:Make:field:serialized:(remove:static)}Value15;
    public readonly int my{on:Make:field:serialized:(remove:readonly)}Value16;
    public static readonly int my{on:Make:field:serialized:(remove:static:and:readonly)}Value17;

    private static int my{on:Make:field:serialized:(remove:static)}Value18;
    private readonly int my{on:Make:field:serialized:(remove:readonly)}Value19;
    private static readonly int my{on:Make:field:serialized:(remove:static:and:readonly)}Value20;

    public static int my{on:Make:field:'myValue21':serialized:(remove:static)}Value21, my{on:Make:field:'myValue22':serialized:(remove:static)}Value22;
    public readonly int my{on:Make:field:'myValue23':serialized:(remove:readonly)}Value23, my{on:Make:field:'myValue24':serialized:(remove:readonly)}Value24;
    public static readonly int my{on:Make:field:'myValue24':serialized:(remove:static:and:readonly)}Value24, my{on:Make:field:'myValue25':serialized:(remove:static:and:readonly)}Value25;

    private static int my{on:Make:field:'myValue26':serialized:(remove:static)}Value26, my{on:Make:field:'myValue27':serialized:(remove:static)}Value27;
    private readonly int my{on:Make:field:'myValue28':serialized:(remove:readonly)}Value28, my{on:Make:field:'myValue29':serialized:(remove:readonly)}Value29;
    private static readonly int my{on:Make:field:'myValue30':serialized:(remove:static:and:readonly)}Value30, my{on:Make:field:'myValue31':serialized:(remove:static:and:readonly)}Value31;

    public static int my{on:Make:all:fields:serialized:(remove:static)}Value32, my{on:Make:all:fields:serialized:(remove:static)}Value33;
    public readonly int my{on:Make:all:fields:serialized:(remove:readonly)}Value34, my{on:Make:all:fields:serialized:(remove:readonly)}Value35;
    public static readonly int my{on:Make:all:fields:serialized:(remove:static:and:readonly)}Value36, my{on:Make:all:fields:serialized:(remove:static:and:readonly)}Value37;

    private static int my{on:Make:all:fields:serialized:(remove:static)}Value38, my{on:Make:all:fields:serialized:(remove:static)}Value39;
    private readonly int my{on:Make:all:fields:serialized:(remove:readonly)}Value40, my{on:Make:all:fields:serialized:(remove:readonly)}Value41;
    private static readonly int my{on:Make:all:fields:serialized:(remove:static:and:readonly)}Value42, my{on:Make:all:fields:serialized:(remove:static:and:readonly)}Value43;

    pub{off}lic i{off}nt myValue44;
    pri{off}vate i{off}nt myValue45;
    int{off}ernal i{off}nt myValue46;
    pub{off}lic sta{off}tic i{off}nt myValue47;
    pub{off}lic rea{off}donly i{off}nt myValue48;
    pub{off}lic sta{off}tic rea{off}donly myValue49;

    pub{off}lic sta{off}tic i{off}nt myValue50;
    pri{off}vate sta{off}tic i{off}nt myValue51;

    pub{off}lic read{off}only i{off}nt myValue52;
    priv{off}ate read{off}only i{off}nt myValue53;
}
