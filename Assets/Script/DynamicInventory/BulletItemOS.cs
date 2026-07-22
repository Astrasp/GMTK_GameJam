using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName =  "BulletObj", menuName = "Scriptable Object/Bullet")]
public class BulletItemOS : ScriptableObject
{
    public string BulletName;
    public AbilityType BulletType;
    public GameObject prefabObj;
    public Color BulletColor;

    [TextArea] public string description;
}

public enum AbilityType
{
    None,
    Gravity,
    Grapple,
    Heal,
    Teleport,
    spuirt
}
