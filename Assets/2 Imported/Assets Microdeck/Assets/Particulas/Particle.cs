using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Particle : MonoBehaviour
{
    ParticleSystem particle;
    public int numero = 1;
    public bool pausa = true;

    public float numero2 = 0;
    // Start is called before the first frame update
    void Start()
    {
        particle = GetComponent<ParticleSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        ParticleSystem.MainModule pmain = particle.main;
        pmain.maxParticles = numero;
        if (pausa)
        {
            particle.Simulate(numero2, true, true);
            pausa = false;
        }
    }
}