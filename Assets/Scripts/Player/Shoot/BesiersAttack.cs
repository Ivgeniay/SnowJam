﻿using Assets.Scripts.Player.Shoot.DTO;
using Assets.Scripts.Player.Weapon.DTO;
using Assets.Scripts.Player.Weapon;
using System.Collections.Generic;
using Assets.Scripts.Utilities;
using System.Linq;
using UnityEngine;
using System;
using Unity.VisualScripting;
using UnityEngine.UIElements;

namespace Assets.Scripts.Player.Shoot
{
    public class BesiersAttack : IShoot
    {
        private ShootingСontrol shootingСontrol;
        private LineRenderer lineRenderer;
        private Transform transform;
        private Transform spawnPoint;
        private Besiers besiers;
        private Projection projection;

        private int maxPhysicsFrameIterationsProjection; 

        private Vector3[] positionPoints;
        private Vector3 curve { get => shootingСontrol.curve; }
        private float throwLength { get => shootingСontrol.throwLength; }

        private Ray _ray = new Ray();
        private bool withAim;

        public BesiersAttack(Transform transform, Transform spawnPoint) {

            this.transform = transform;
            this.spawnPoint = spawnPoint;
            this.lineRenderer = transform.GetComponent<LineRenderer>();
            this.shootingСontrol = transform.GetComponent<ShootingСontrol>();
            this.projection = transform.GetComponent<Projection>();

            maxPhysicsFrameIterationsProjection = projection.maxPhysicsFrameIterations;
            projection.OnMaxPhysicsFrameIterationsChanged += OnMaxPhysicsFrameIterationsChanged;

            besiers = new Besiers();
            positionPoints = new Vector3[3];
        }

        ~BesiersAttack() {
            projection.OnMaxPhysicsFrameIterationsChanged -= OnMaxPhysicsFrameIterationsChanged;
        }
        

        public void GetAim(AimDTO aimDTO)
        {
            withAim = true;

            DeterminationPositions(curve);

            //lineRenderer.ResetBounds();
            lineRenderer.positionCount = maxPhysicsFrameIterationsProjection;
            lineRenderer.SetPosition(0, spawnPoint.position);

            for (var i = 1; i < maxPhysicsFrameIterationsProjection; i++)
            {
                float t = 0.01f * i;
                var resultVector = besiers.GetPoint(positionPoints, t);
                lineRenderer.SetPosition(i, resultVector);
            }
        }
        private void DeterminationPositions(Vector3 curve)
        {
            if (positionPoints.Count() < 2) throw new Exception("Points in array are less than 2");

            positionPoints[0] = spawnPoint.transform.position;
            positionPoints[1] = transform.TransformPoint(spawnPoint.transform.localPosition + curve);

            _ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.forward, Color.red, throwLength);

            Physics.Raycast(_ray, out RaycastHit hitinfo, throwLength);

            if (hitinfo.collider is not null)
            {
                positionPoints[2] = hitinfo.point;
                Debug.DrawLine(_ray.origin, positionPoints[2], Color.blue);
            }
            else
            {
                var currentDistance = throwLength;
                var localPoint = new Vector3(_ray.direction.x * currentDistance, _ray.direction.y * currentDistance, _ray.direction.z * currentDistance);
                positionPoints[2] = _ray.origin + localPoint;
                Debug.DrawLine(_ray.origin, positionPoints[2]);
            }
        }

        public void GetAttack(AttackDTO attackDTO)
        {
            if (IsTargetCloserThanPlayer(positionPoints[0], positionPoints[2], transform.position)) return;
            if (withAim == false) DeterminationPositions(Vector3.zero);
            

            var instance = Instantiator.Instantiate(attackDTO.Weapon.GetPrefab(), spawnPoint.position, attackDTO.SpawnPoint.rotation);

            var instanceScr = instance.GetComponent<IWeapon>();
            instanceScr.SetCreator(transform);

            var nonPhy = instance.GetComponent<INonPhysicWeapon>();
            nonPhy.ItineraryPoints = positionPoints;

            Coroutines.Start(nonPhy.SetNonPhyMove(new NonPhysicParameters() 
                {
                    delaySecond = shootingСontrol.frameDelayNonPhysics * 0.001f,
                    step = shootingСontrol.StepNonPhysic * 0.001f,
                    t = 0
                }));

            withAim = false;
        }

        private bool IsTargetCloserThanPlayer(Vector3 viewPointFrom, Vector3 viewPointTo, Vector3 player) {
            var headingToPlayer = player - viewPointFrom;
            var headingToTarger = player - viewPointTo;
            return headingToPlayer.magnitude > headingToTarger.magnitude;
        }

        private void OnMaxPhysicsFrameIterationsChanged(int obj) => maxPhysicsFrameIterationsProjection = obj;
    }
}

