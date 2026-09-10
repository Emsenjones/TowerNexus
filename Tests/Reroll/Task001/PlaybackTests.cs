using System;
using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using UnityEngine;
class PlaybackTests
{
    static int checks;
    static void Check(bool value,string label) { if(!value) throw new Exception(label);checks++; }
    static void Near(float a,float b,string label) => Check(Math.Abs(a-b)<0.0001f,label+": "+a+" != "+b);
    static void Set(object obj,string name,object value) => obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(obj,value);
    static T Get<T>(object obj,string name) => (T)obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(obj);
    static void Call(object obj,string name) => obj.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(obj,null);
    static UIAnimationStep Fade(float start,float end,float duration,float delay=0) => UIAnimationStep.CreateFadeStep(start,end,duration,delay,Ease.Linear);
    static UIAnimationPlayer Player(params UIAnimationStep[] steps)
    {
        var p=new UIAnimationPlayer();Set(p,"animatedRoot",new RectTransform());Set(p,"canvasGroup",new CanvasGroup());
        Set(p,"animationSteps",new List<UIAnimationStep>(steps));return p;
    }
    static void Play(UIAnimationPlayer p,Action end=null) { Check(p.TryPlay(end,out string reason),reason); }
    static void Tick(UIAnimationPlayer p,float unscaled,float scaled=-1)
    { Time.frameCount++;Time.unscaledDeltaTime=unscaled;Time.deltaTime=scaled<0?unscaled:scaled;p.Tick(Time.deltaTime,Time.unscaledDeltaTime); }
    static float Alpha(UIAnimationPlayer p) => Get<CanvasGroup>(p,"canvasGroup").alpha;
    static void Invalid(UIAnimationStep s,string label)
    { var p=Player(s);Check(!p.TryValidate(out string reason)&&reason.Length>0,label); }
    static void Main()
    {
        // Reverse list order, common endpoint, explicit subsequent start, and overshot boundaries.
        var p=Player(Fade(1,0,.5f,.5f),Fade(0,1,.5f));int completed=0;
        Play(p,()=>completed++);Near(Alpha(p),0,"earliest fade initializes");
        Tick(p,.25f);Near(Alpha(p),.5f,"fade in");Tick(p,.25f);Near(Alpha(p),1,"shared endpoint");
        Tick(p,.25f);Near(Alpha(p),.5f,"fade out");Tick(p,.25f);Near(Alpha(p),0,"final alpha");Check(completed==1&&!p.IsPlaying,"complete once");
        Tick(p,1);Check(completed==1,"no repeated completion");
        Play(p);Tick(p,.8f);Near(Alpha(p),.4f,"cross multiple boundaries");Tick(p,10);Near(Alpha(p),0,"overshoot finishes exactly");
        p=Player(Fade(0,1,.2f),Fade(.6f,0,.2f,.8f));Play(p);Tick(p,.6f);Near(Alpha(p),1,"gap holds preceding state");Tick(p,.2f);Near(Alpha(p),.6f,"later explicit start");
        // Parallel channels, overshooting Ease does not clamp scale, untouched external root.
        p=Player(Fade(0,1,1),UIAnimationStep.CreatePositionStep(new Vector2(0,0),new Vector2(0,30),1,Ease.Linear),UIAnimationStep.CreateScaleStep(Vector3.one,new Vector3(2,2,2),1,0,Ease.OutBack));
        Play(p);Tick(p,.5f);Near(Get<RectTransform>(p,"animatedRoot").anchoredPosition.y,15,"parallel position");Check(Get<RectTransform>(p,"animatedRoot").localScale.x>2,"ease overshoot preserved");
        // Replay in each visual phase and failed replay cancels old completion.
        foreach(float phase in new[]{.1f,.5f,.9f})
        {
            p=Player(Fade(0,1,.2f),Fade(1,0,.2f,.8f));int old=0,fresh=0;Play(p,()=>old++);Tick(p,phase);Play(p,()=>fresh++);Near(Alpha(p),0,"replay resets");Tick(p,1);Check(old==0&&fresh==1,"only new completion");
        }
        p=Player(Fade(0,1,1));completed=0;Play(p,()=>completed++);Set(p,"animationSteps",new List<UIAnimationStep>());Check(!p.TryPlay(()=>completed++,out _),"empty replay rejected");Tick(p,2);Check(!p.IsPlaying&&completed==0,"invalid replay cancelled old callback");
        // Raw validation and overflow, disabled list, missing targets, conflicts.
        Invalid(Fade(0,1,-1),"negative raw duration");Invalid(Fade(0,1,1,-1),"negative delay");
        Invalid(Fade(0,1,float.NaN),"NaN duration");Invalid(Fade(0,1,float.MaxValue,float.MaxValue),"sum overflow");Invalid(Fade(float.NaN,1,1),"NaN alpha");Invalid(Fade(-1,1,1),"unclamped alpha");
        var disabled=Fade(0,1,1);Set(disabled,"enabled",false);Check(!Player(disabled).TryValidate(out _),"all disabled");
        p=Player(Fade(0,1,1));Set(p,"canvasGroup",null);Check(!p.TryValidate(out _),"missing opacity target");
        p=Player(Fade(0,1,1));Set(p,"animatedRoot",null);Check(p.TryValidate(out _),"fade only needs opacity");
        Check(!Player(Fade(0,1,1),Fade(1,0,1,.5f)).TryValidate(out _),"overlap rejected");
        Check(!Player(Fade(0,1,0),Fade(1,0,0)).TryValidate(out _),"same time impulse ambiguous");
        // Zero duration and cancellation before its deferred completion; scaled freeze.
        p=Player(Fade(0,1,0));completed=0;Play(p,()=>completed++);Check(completed==0&&p.IsPlaying,"zero duration not synchronous");
        p.Tick(Time.deltaTime,Time.unscaledDeltaTime);Check(completed==0,"same frame cannot finish");Tick(p,0);Check(completed==0,"zero delta not effective update");Tick(p,.1f);Check(completed==1,"zero finishes next effective update");Near(Alpha(p),1,"zero target");
        Play(p,()=>completed++);p.Cancel();Tick(p,1);Check(completed==1,"cancel revokes zero completion");
        p=Player(Fade(0,1,1));Set(p,"timeMode",UIAnimationTimeMode.Scaled);Play(p);Tick(p,.5f,0);Near(Alpha(p),0,"scaled paused");Tick(p,.1f,.5f);Near(Alpha(p),.5f,"scaled speed");
        p=Player(Fade(0,1,1));Play(p);Tick(p,.5f,0);Near(Alpha(p),.5f,"unscaled paused battle");
        Tick(p,.1f,.4f);Near(Alpha(p),.6f,"unscaled ignores speed");
        // Completion reentrancy, disable, and active playback snapshots.
        p=Player(Fade(0,1,.1f));var captured=p;completed=0;Play(p,()=>{completed++;Play(captured,()=>completed++);});Tick(p,.1f);Check(p.IsPlaying&&completed==1,"completion can replay");Tick(p,.1f);Check(completed==2&&!p.IsPlaying,"replayed completion retained");
        var host=new ToastUI();Set(host,"messageText",new TMPro.TextMeshProUGUI());Set(host,"animationPlayer",p);
        Check(host.TryPlay("test",x=>completed++,out _),"host play");host.gameObject.SetActive(false);Tick(p,1);Check(completed==2&&!p.IsPlaying,"host disable cancels");Check(!host.TryPlay("test",null,out _),"inactive host rejected");
        var authored=Fade(0,1,1);p=Player(authored);Play(p);Set(authored,"targetAlpha",0f);Tick(p,.5f);Near(Alpha(p),.5f,"snapshot isolated from live authoring");
        // Delayed zero at preceding endpoint is legal even when listed first.
        p=Player(Fade(.3f,.7f,0,.5f),Fade(0,1,.5f));Play(p);Tick(p,.5f);Near(Alpha(p),.7f,"endpoint zero target wins");
        // Owners: failed play, early disable/destroy, manager disable clear registration.
        var manager=new DamageNumberManager();var list=Get<List<DamageNumberUI>>(manager,"createdDamageNumbers");
        var number=new DamageNumberUI();list.Add(number);number.Unavailable+=(Action<DamageNumberUI>)Delegate.CreateDelegate(typeof(Action<DamageNumberUI>),manager,typeof(DamageNumberManager).GetMethod("HandleDamageNumberComplete",BindingFlags.Instance|BindingFlags.NonPublic));
        Call(number,"OnDisable");Check(list.Count==0&&!number.gameObject.active,"early number disable unregisters and removes");
        number=new DamageNumberUI();list.Add(number);Call(manager,"OnDisable");Check(list.Count==0&&!number.gameObject.active,"manager disable clears children");Call(manager,"OnDestroy");Check(list.Count==0,"cleanup idempotent");
        Set(manager,"damageNumberPrefab",new DamageNumberUI());number=new DamageNumberUI();UnityEngine.Object.Factory=()=>number;manager.ShowDamage(10,new Vector3());Check(list.Count==0&&!number.gameObject.active,"failed playback does not leak");
        number=new DamageNumberUI();list.Add(number);number.Unavailable+=(Action<DamageNumberUI>)Delegate.CreateDelegate(typeof(Action<DamageNumberUI>),manager,typeof(DamageNumberManager).GetMethod("HandleDamageNumberComplete",BindingFlags.Instance|BindingFlags.NonPublic));
        Call(number,"OnDestroy");Check(list.Count==0,"early destruction unregisters");Call(number,"OnDestroy");Check(list.Count==0,"repeated unavailable notification harmless");
        // Damage text/placement stays on the outer object; preview does not add random displacement.
        number=new DamageNumberUI();p=Player(Fade(0,1,.4f),UIAnimationStep.CreatePositionStep(new Vector2(),new Vector2(0,30),.4f,Ease.Linear));Set(p,"timeMode",UIAnimationTimeMode.Scaled);
        var damageText=new TMPro.TextMeshProUGUI();Set(number,"damageText",damageText);Set(number,"animationPlayer",p);number.gameObject.transform.anchoredPosition=new Vector2(100,200);
        Check(number.TryPlay(42,null,out _),"damage play");Near(number.gameObject.transform.anchoredPosition.x,99,"random offset outer only");Check(damageText.text=="42","damage text preserved");Tick(p,.4f);Near(number.gameObject.transform.anchoredPosition.y,199,"animation leaves outer spawn intact");
        number.Preview();Time.frameCount++;Time.deltaTime=.4f;Time.unscaledDeltaTime=.1f;Call(number,"Update");Check(!p.IsPlaying,"damage host Update drives scaled playback");Near(number.gameObject.transform.anchoredPosition.y,199,"preview does not accumulate random offset");
        // Zero-duration replay replaces the pending callback rather than firing both.
        p=Player(Fade(0,1,0));int zeroOld=0,zeroNew=0;Play(p,()=>zeroOld++);Play(p,()=>zeroNew++);Tick(p,.1f);Check(zeroOld==0&&zeroNew==1,"zero replay revokes old completion");
        // Toast forwards failure/replay and enforces non-blocking graphics.
        p=Player(Fade(0,1,1));var toast=new ToastUI();var text=new TMPro.TextMeshProUGUI();var cg=new CanvasGroup();toast.gameObject.members.Add(text);toast.gameObject.members.Add(cg);Set(toast,"messageText",text);Set(toast,"animationPlayer",p);
        completed=0;Check(toast.TryPlay("hello",x=>completed++,out _),"toast play");Check(text.text=="hello"&&!text.raycastTarget&&!cg.blocksRaycasts&&!cg.interactable,"toast presentation");Time.frameCount++;Time.unscaledDeltaTime=1;Time.deltaTime=0;Call(toast,"Update");Check(completed==1,"host Update drives unscaled completion");
        Check(toast.TryPlay("again",x=>completed++,out _),"toast replay");Set(toast,"messageText",null);Check(!toast.TryPlay("bad",x=>completed++,out _),"toast failed replay");Tick(p,2);Check(completed==1&&!p.IsPlaying,"toast failure cancelled old");
        Check(typeof(UIAnimationPlayer).IsSerializable && !typeof(MonoBehaviour).IsAssignableFrom(typeof(UIAnimationPlayer)),"serializable ordinary class");
        Check(Get<UIAnimationPlayer>(new DamageNumberUI(),"animationPlayer").TimeMode==UIAnimationTimeMode.Scaled,"damage inline default scaled");
        Check(Get<UIAnimationPlayer>(new ToastUI(),"animationPlayer").TimeMode==UIAnimationTimeMode.Unscaled,"toast inline default unscaled");
        Console.WriteLine("PASS: "+checks+" managed assertions executing production player and owner code.");
    }
}
