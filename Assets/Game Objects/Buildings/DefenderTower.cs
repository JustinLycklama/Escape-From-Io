using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class DefenderTower : RotatingBuilding {

    public override string title => "Defense Turret";

    public override float constructionModifierSpeed => 0.2f;

    [SerializeField]
    private Animator weaponAnimator = null;

    private Unit target;
    private UnitManager unitManager;

    private AudioManager audioManager;

    private float lastShotTime = 0;
    private const float shotCooldown = 2f;

    private const float range = 275;
    private const int damage = 25;

    [SerializeField]
    private ParticleSystem[] particles = new ParticleSystem[0];

    protected override void Start() {
        base.Start();

        unitManager = Script.Get<UnitManager>();
        audioManager = Script.Get<AudioManager>();
    }

    protected override void CompleteBuilding() {
        StartCoroutine(CheckForTarget());
        StartCoroutine(RotateToTarget());
    }

    protected override void UpdateCompletionPercent(float percent) {
    }

    private IEnumerator CheckForTarget() {

        while (true) {
            yield return new WaitForSeconds(1);

            if (target != null) {
                continue;
            }

            Unit[] enemyList = unitManager.GetAllPlayerUnits(Unit.FactionType.Enemy);

            if (enemyList.Count() == 0) {
                continue;
            }

            Unit closestUnit = null;
            float closestDistance = float.MaxValue;

            foreach(Unit unit in enemyList) {
                float distance = Vector3.Distance(unit.transform.position, transform.position);

                if (distance < closestDistance) {
                    closestUnit = unit;
                    closestDistance = distance;
                }
            }

            if(closestDistance < range) {
                target = closestUnit;
            }
        }
    }

    private IEnumerator RotateToTarget() {

        while(true) {
            yield return new WaitForSeconds(1);

            if (target == null) {
                continue;
            }


            Action shootAction = () => {
                if (Time.time > lastShotTime + shotCooldown) {
                    lastShotTime = Time.time;

                    if (unitManager.IsUnitEnabled(target) && Vector3.Distance(target.transform.position, transform.position) < range) {

                        target.TakeDamage(damage);

                        weaponAnimator.Play("Recoil_Single");

                        audioManager.PlayAudio(AudioManager.Type.TurretShot, transform.position);

                        for(int i = 0; i < particles.Length; i++) {
                            particles[i].Play();
                        }


                    }







                }
            };

            Quaternion targetRotation = GetRotationQuad(target.transform.position);
            Quaternion originalRotation = rotatingComponent.transform.rotation;

            float degreesToTurn = (targetRotation.eulerAngles - originalRotation.eulerAngles).magnitude;
            AttemptToRotate(targetRotation, shootAction);                  
        }        
    }
}
