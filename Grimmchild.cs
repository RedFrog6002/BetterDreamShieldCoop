using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Satchel;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using FrogCore;

namespace BetterDreamShieldCoop
{
    internal class Grimmchild : MonoBehaviour, ICoop
    {
        internal static GameObject prefab;
        internal static tk2dSpriteCollectionData collection;
        internal static tk2dSpriteAnimation animation;

        public static void CreatePrefab()
        {
            if (!prefab)
            {
                GameObject grimm = HeroController.instance.gameObject.FindGameObjectInChildren("Charm Effects")
                    .LocateMyFSM("Spawn Grimmchild").GetAction<SpawnObjectFromGlobalPool>("Spawn", 2)
                    .gameObject.Value.FindGameObjectInChildren("Grimmchild");
                prefab = Instantiate(grimm);
                DontDestroyOnLoad(prefab);
                tk2dSprite sprite = prefab.GetComponent<tk2dSprite>();
                tk2dSpriteAnimator animator = prefab.GetComponent<tk2dSpriteAnimator>();
                collection = Utils.CloneTk2dCollection(sprite.Collection, "Friendgrimm Col");
                animation = Utils.CloneTk2dAnimation(animator.Library, "Friendgrimm Anim");
                animation.SetCollection(collection);
                animator.Library = animation;
                prefab.AddComponent<Grimmchild>();
                prefab.SetActive(false);
                prefab.name = "friendgrimm";
                prefab.tag = "Untagged";
            }
        }

        public static Grimmchild Create()
        {
            GameObject instance = Instantiate(prefab, HeroController.instance.transform.position, Quaternion.Euler(0f, 0f, 0f));
            instance.SetActive(true);
            DontDestroyOnLoad(instance);
            return instance.GetComponent<Grimmchild>();
        }

        public static string GetOptionName(string option) => option switch
        {
            "Special1" => "Force Attack",
            "Special2" => "Disable Attacking",
            "Special3" => "Change Speed",
            "Special4" => "Toggle original AI",
            _ => option
        };

        private const float speed = 8f;
        private float speedMult = 1f;
        private bool slow = false;
        private bool altMode = false;
        private bool canAttack = true;
        private float scaledSpeed => Time.deltaTime * speed * speedMult;

        private tk2dSprite _sprite;
        private PlayMakerFSM _control;
        private Rigidbody2D _rb2d;

        private void Awake()
        {
            _sprite = GetComponent<tk2dSprite>();
            _control = gameObject.LocateMyFSM("Control");
            _rb2d = GetComponent<Rigidbody2D>();
            FsmState follow = _control.GetState("Follow");
            FsmState noAI = _control.AddState("No AI");
            GrimmChildFly fly = follow.GetAction<GrimmChildFly>(5);
            follow.AddCustomAction(() => { if (!altMode) _control.SetState("No AI"); });
            follow.AddAction(new GrimmChildFly() { 
                objectA = fly.objectA, objectB = fly.objectB, spriteFacesRight = true, playNewAnimation = true, 
                newAnimationClip = "TurnFly Anim", resetFrame = true, fastAnimSpeed = 10f, fastAnimationClip = "Fast Anim", 
                normalAnimationClip = "Fly Anim", pauseBetweenAnimChange = 0.3f, flyingFast = false });
            _control.InsertCustomAction("Antic", () => { if (!canAttack) _control.SetState("Follow"); }, 0);
            _control.Fsm.GlobalTransitions = new FsmTransition[0];
        }

        public void Up(bool held)
        {
            if (!altMode)
                transform.position += new Vector3(0f, scaledSpeed);
        }

        public void Down(bool held)
        {
            if (!altMode)
                transform.position -= new Vector3(0f, scaledSpeed);
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
            if (!altMode)
            {
                transform.position = HeroController.instance.transform.position;
                if (!held)
                    HeroController.instance.shadowRingPrefab.Spawn(transform.position);
            }
        }

        public void Special1(bool held)
        {
            if (!held)
                _control.SetState("Check For Target");
        }

        public void Special2(bool held)
        {
            if (!held)
                canAttack = !canAttack;
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
                if (altMode)
                {
                    if (_control.ActiveStateName == "No AI")
                        _control.SetState("Follow");
                }
                else if (_control.ActiveStateName == "Follow")
                    _control.SetState("No AI");
            }
        }

        private void Update()
        {
            if (!altMode)
                _rb2d.velocity = Vector2.zero;
        }

        public void DestroyCoop()
        {
            Destroy(gameObject);
        }

        private Coroutine _colCo;

        private IEnumerator ColorChange()
        {
            _sprite.color = slow ? Color.green : Color.blue;
            yield return new WaitForSeconds(1f);
            _sprite.color = Color.white;
        }
    }
}
