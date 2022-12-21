using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseMaterialMapper : ScriptableObject
{
    public abstract Dictionary<Component, Material> GetMaterialMap(GameObject gameObject);
}
