using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Satchel;
using HutongGames.PlayMaker.Actions;
using FrogCore;

namespace BetterDreamShieldCoop
{
    internal class Dreamshield : MonoBehaviour, ICoop
    {
        internal static GameObject prefab;
        internal static tk2dSpriteCollectionData collection;
        internal static tk2dSpriteAnimation animation;

        public static void CreatePrefab()
        {
            if (!prefab)
            {
                GameObject shield = HeroController.instance.gameObject.FindGameObjectInChildren("Charm Effects")
                    .LocateMyFSM("Spawn Orbit Shield").GetAction<SpawnObjectFromGlobalPool>("Spawn", 2)
                    .gameObject.Value.FindGameObjectInChildren("Shield");
                prefab = Instantiate(shield);
                DontDestroyOnLoad(prefab);
                tk2dSprite sprite = prefab.GetComponent<tk2dSprite>();
                tk2dSpriteAnimator animator = prefab.GetComponent<tk2dSpriteAnimator>();
                collection = Utils.CloneTk2dCollection(sprite.Collection, "Friendshield Col");
                animation = Utils.CloneTk2dAnimation(animator.Library, "Friendshield Anim");
                animation.SetCollection(collection);
                sprite.Collection = collection;
                animator.Library = animation;
                prefab.AddComponent<Dreamshield>();
                prefab.SetActive(false);
                prefab.name = "friendshield";
                prefab.tag = "Untagged";
            }
        }

        public static Dreamshield Create()
        {
            GameObject instance = Instantiate(prefab, HeroController.instance.transform.position, Quaternion.Euler(0f, 0f, 90f));
            instance.SetActive(true);
            DontDestroyOnLoad(instance);
            return instance.GetComponent<Dreamshield>();
        }

        public static string GetOptionName(string option) => option switch
        {
            "Teleport" => "Teleport/Reset",
            "Special1" => "Rotate Left",
            "Special2" => "Rotate Right",
            "Special3" => "Change Speed",
            "Special4" => "Rotate around player",
            _ => option
        };

        private const float speed = 8f;
        private const float rotSpeed = 110f;
        private float speedMult = 1f;
        private float distance = 3f;
        private bool slow = false;
        private bool altMode = false;
        private float scaledSpeed => Time.deltaTime * speed * speedMult;
        private float scaledRotSpeed => Time.deltaTime * rotSpeed * speedMult;

        private tk2dSprite _sprite;

        private void Awake()
        {
            _sprite = GetComponent<tk2dSprite>();
            On.NailSlash.OnTriggerEnter2D += SlashHit;
        }

        private void SlashHit(On.NailSlash.orig_OnTriggerEnter2D orig, NailSlash self, Collider2D otherCollider)
        {
            if (otherCollider.gameObject == gameObject)
                HeroController.instance.Bounce();
            orig(self, otherCollider);
        }

        public void Up(bool held)
        {
            if (!altMode)
                transform.position += new Vector3(0f, scaledSpeed);
            else
                distance += scaledSpeed;
        }

        public void Down(bool held)
        {
            if (!altMode)
                transform.position -= new Vector3(0f, scaledSpeed);
            else
                distance -= scaledSpeed;
        }

        public void Left(bool held)
        {
            if (!altMode)
                transform.position -= new Vector3(scaledSpeed, 0f);
        }

        public void Right(bool held)
        {
            if (!altMode)
                transform.position += new Vector3(scaledSpeed, 0f);
        }

        public void Teleport(bool held)
        {
            transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            distance = 3f;
            if (!altMode)
                transform.position = HeroController.instance.transform.position;
            else
                transform.position = HeroController.instance.transform.position + new Vector3(0f, distance);
            if (!held)
                HeroController.instance.shadowRingPrefab.Spawn(transform.position);
        }

        public void Special1(bool held)
        {
            transform.localRotation = Quaternion.Euler(0f, 0f, transform.localRotation.eulerAngles.z + scaledRotSpeed);
        }

        public void Special2(bool held)
        {
            transform.localRotation = Quaternion.Euler(0f, 0f, transform.localRotation.eulerAngles.z - scaledRotSpeed);
        }

        public void Special3(bool held)
        {
            if (!held)
            {
                if (_colCo != null)
                    StopCoroutine(_colCo);
                slow = !slow;
                speedMult = slow ? 0.5f : 1f;
                _colCo = StartCoroutine(ColorChange());
            }
        }

        public void Special4(bool held)
        {
            if (!held)
            {
                altMode = !altMode;
                HeroController.instance.shadowRingPrefab.Spawn(transform.position);
            }
        }

        public void DestroyCoop()
        {
            On.NailSlash.OnTriggerEnter2D -= SlashHit;
            Destroy(gameObject);
        }

        private Coroutine _colCo;

        private IEnumerator ColorChange()
        {
            _sprite.color = slow ? Color.green : Color.blue;
            yield return new WaitForSeconds(1f);
            _sprite.color = Color.white;
        }

        private void Update()
        {
            if (altMode)
            {
                transform.position = HeroController.instance.transform.position + new Vector3(Mathf.Cos(transform.rotation.eulerAngles.z * Mathf.PI / 180f) * distance, Mathf.Sin(transform.rotation.eulerAngles.z * Mathf.PI / 180f) * distance);
            }
        }
    }
}
