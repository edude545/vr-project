using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class Bird : WaggleTrigger {

    public float FlySpeed = 0.01f;
    
    public Animator animator;

    public GameObject Target; // most birds target crops, aggressive birds target the player
    public bool IsAggressive = false;
    public Plant FavoritePlant;

    public bool Fleeing = false;

    public float InteractRange = 2f;
    public float DespawnRange = 200f;
    public float FleeSpeed = 20f;

    public float DamageDealt = 0.1f;

    public float SpawnInterval = 1.6f;
    public float SpawnDistance = 100f;

    public Vector3 Velocity;

    public States State;
    public enum States {
        Flying,
        Eating,
        Attacking,
        Fleeing
    }

    private void Start() {
        ChangeState(States.Flying);
    }

    public void ChangeState(States state) {
        State = state;
        if (state == States.Flying) {
            animator.SetBool("eating", false);
            animator.SetBool("attacking", false);
            findTarget();
        } else if (state == States.Eating) {
            animator.SetBool("eating", true);
            animator.SetBool("attacking", false);
        } else if (state == States.Attacking) {
            animator.SetBool("eating", false);
            animator.SetBool("attacking", true);
        } else if (state == States.Fleeing) {
            animator.SetBool("eating", false);
            animator.SetBool("attacking", false);
        }
    }

    private void Update() {
        if (State == States.Flying) {
            Vector3 velocity = (Target.transform.position - transform.position).normalized * FlySpeed;
            transform.position += velocity;
            if ((transform.position - Target.transform.position).magnitude <= InteractRange) {
                if (Target.Equals(GameController.Instance.Head)) {
                    ChangeState(States.Attacking);
                } else {
                    ChangeState(States.Eating);
                }
            }
            transform.rotation = Quaternion.LookRotation(velocity);
        } else if (State == States.Eating) {
            Target.GetComponent<Crop>().TakeDamage(DamageDealt);
            if (Target.GetComponent<Crop>().IsKilled())
            {
                ChangeState(States.Fleeing);
            }
        } else if (State == States.Attacking) {
            GameController.Instance.ChangeBodySize(-DamageDealt);
        } else if (State == States.Fleeing) {
            transform.position += FleeSpeed * transform.position.normalized;
        }
        if (transform.position.magnitude > DespawnRange) {
            Destroy(gameObject);
        }
        updateWaggleScore();
        if (Waggle > MaxWaggle)
        {
            ChangeState(States.Fleeing);
        }
    }

    void findTarget() {
        if (IsAggressive || GameController.Instance.LiveCrops.childCount == 0) {
            Target = GameController.Instance.Head;
        } else {
            float[] weights = new float[GameController.Instance.LiveCrops.transform.childCount];
            Crop[] values = new Crop[GameController.Instance.LiveCrops.transform.childCount];
            for (int i = 0; i < weights.Length; i++) {
                Crop crop = GameController.Instance.LiveCrops.transform.GetChild(i).GetComponent<Crop>();
                // Birds are 3 times more likely to choose their favorite crop, and less likely to choose damaged crops.
                weights[i] = (crop.PlantType == FavoritePlant ? 3f : 1f) * (crop.HP / crop.PlantType.MaxHP);
                Debug.Log("Added weight " + weights[i] + " for " + crop.PlantType.Name);
                values[i] = crop;
            }
            Debug.Log("Making weighted random chooser...");
            WeightedRandom<Crop> crops = new WeightedRandom<Crop>(weights, values);
            Target = crops.Choose().gameObject;
        }
    }

}
