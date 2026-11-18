using System.Collections;
using System.Collections.Generic;
using SYPackages.PerObjectShadow;
using Unity.Collections;
using UnityEngine;

[ExecuteInEditMode]
public class PerObjectShadowFixBugScripts : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(BeginFixed());
    }

    IEnumerator BeginFixed()
    {
        yield return new WaitForSeconds(0.5f);
        PerObjectShadowRenderers renderer = this.GetComponent<PerObjectShadowRenderers>();
        renderer.SetUpPOSRenderers();
        PerObjectShadowEntity shadowEntity = this.GetComponent<PerObjectShadowEntity>();
        shadowEntity.SetUpPOSRenderers();
    }
}
