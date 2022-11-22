﻿using Assets.Scripts.Enemies.DamageMech;
using Assets.Scripts.EventArgs;
using Assets.Scripts.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Enemies.StateMech.States
{
    public class SnowmanDie : IState
    {
        private Transform transform;
        private List<IDestroyable> destroyableParts;
        private List<IDamageable> damagebleParts;
        private Animator animator;
        public SnowmanDie(Transform transform, Animator animator) 
        { 
            this.transform = transform; 
            this.animator = animator;
        }
        public void Start() {
            animator.speed = 0;
            destroyableParts = new List<IDestroyable>();
            destroyableParts = transform.GetComponentsInChildren<IDestroyable>().ToList();
            damagebleParts = transform.GetComponentsInChildren<IDamageable>().ToList();
            destroyableParts.ForEach(x => x.Destroy());
            damagebleParts.ForEach(x => x.Destroy());
            animator.speed = 0.1f;
            Coroutines.Start(Destroy());
        }
        public void Update() {
        }
        public void Exit() {
        }

        private IEnumerator Destroy()
        {
            yield return new WaitForSeconds(1);
            transform.gameObject.SetActive(false);
        }
    }
}
