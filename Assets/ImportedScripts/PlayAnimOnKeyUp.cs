using UnityEngine;

public class PlayAnimOnKeyUp : MonoBehaviour {

    public GameObject mainProjectile;
    public ParticleSystem mainParticleSystem;

	void Update ()
    {
        mainProjectile.SetActive(mainParticleSystem.IsAlive() && Input.GetKeyUp(KeyCode.Space));
	}
}
