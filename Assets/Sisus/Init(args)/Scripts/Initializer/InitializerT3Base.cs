#pragma warning disable CS0414

using System;
using JetBrains.Annotations;
#if UNITY_EDITOR
using Sisus.Init.EditorOnly;
#endif
using UnityEngine;
using Object = UnityEngine.Object;
using static Sisus.Init.Internal.InitializerUtility;
using static Sisus.NullExtensions;

namespace Sisus.Init
{
	/// <summary>
	/// A base class for a component that can can specify the three arguments used to
	/// initialize an object that implements
	/// <see cref="IInitializable{TFirstArgument, TSecondArgument, TThirdArgument}"/>.
	/// <para>
	/// The arguments get injected to the <typeparamref name="TClient">client</typeparamref>
	/// during the <see cref="Awake"/> event.
	/// </para>
	/// <para>
	/// The client receives the arguments via the
	/// <see cref="IInitializable{TFirstArgument, TSecondArgument, TThirdArgument}.Init">Init</see>
	/// method where it can assigned them to member fields or properties.
	/// </para>
	/// <para>
	/// After the arguments have been injected the <see cref="Initializer{,,,}"/> is removed from the
	/// <see cref="GameObject"/> that holds it.
	/// </para>
	/// <para>
	/// When you derive your Initializer class from <see cref="InitializerBase{,,,}"/>
	/// you are responsible for implementing the argument properties and serializing their value.
	/// This means you will need to write a little bit more code, but it also grants you more options
	/// in how to handle the serialization, making it possible to support types that Unity can't serialize
	/// automatically. If you derive from <see cref="Initializer{,,,}"/> instead, then these things will be handled for you.
	/// </para>
	/// </summary>
	/// <typeparam name="TClient"> Type of the initialized client component. </typeparam>
	/// <typeparam name="TFirstArgument"> Type of the first argument to pass to the client component's Init function. </typeparam>
	/// <typeparam name="TSecondArgument"> Type of the second argument to pass to the client component's Init function. </typeparam>
	/// <typeparam name="TThirdArgument"> Type of the third argument to pass to the client component's Init function. </typeparam>
	public abstract class InitializerBase<TClient, TFirstArgument, TSecondArgument, TThirdArgument> : MonoBehaviour
		, IInitializer<TClient, TFirstArgument, TSecondArgument, TThirdArgument>, IValueProvider<TClient>
		#if UNITY_EDITOR
		, IInitializerEditorOnly
		#endif
		where TClient : MonoBehaviour, IInitializable<TFirstArgument, TSecondArgument, TThirdArgument>
	{
		[SerializeField, HideInInspector, Tooltip(TargetTooltip)]
		protected TClient target = null;

		[SerializeField, HideInInspector, Tooltip(NullArgumentGuardTooltip)]
		private NullArgumentGuard nullArgumentGuard = NullArgumentGuard.EditModeWarning | NullArgumentGuard.RuntimeException;

		/// <inheritdoc/>
		TClient IValueProvider<TClient>.Value => target;

		/// <inheritdoc/>
		object IValueProvider.Value => target;

		/// <inheritdoc/>
		Object IInitializer.Target { get => target; set => target = (TClient)value; }

		/// <inheritdoc/>
		bool IInitializer.TargetIsAssignableOrConvertibleToType(Type type) => type.IsAssignableFrom(typeof(TClient));

		/// <inheritdoc/>
		object IInitializer.InitTarget() => InitTarget();

		/// <summary>
		/// The first argument passed to the <typeparamref name="TClient">client</typeparamref>'s Init function.
		/// </summary>
		protected abstract TFirstArgument FirstArgument { get; set; }

		/// <summary>
		/// The second argument passed to the <typeparamref name="TClient">client</typeparamref>'s Init function.
		/// </summary>
		protected abstract TSecondArgument SecondArgument { get; set; }

		/// <summary>
		/// The third argument passed to the <typeparamref name="TClient">client</typeparamref>'s Init function.
		/// </summary>
		protected abstract TThirdArgument ThirdArgument { get; set; }

		#if UNITY_EDITOR
		NullArgumentGuard IInitializerEditorOnly.NullArgumentGuard { get => nullArgumentGuard; set => nullArgumentGuard = value; }
		string IInitializerEditorOnly.NullGuardFailedMessage { get => nullGuardFailedMessage; set => nullGuardFailedMessage = value; }
		bool IInitializerEditorOnly.HasNullArguments => HasNullArguments;
		protected virtual bool HasNullArguments => IsNull(FirstArgument) || IsNull(SecondArgument) || IsNull(ThirdArgument);
		[HideInInspector, NonSerialized] private string nullGuardFailedMessage = "";
		bool IInitializerEditorOnly.MultipleInitializersPerTargetAllowed => false;
		#endif

		/// <inheritdoc/>
		public TClient InitTarget()
		{
			if(this == null)
			{
				return target;
			}

			var firstArgument = FirstArgument;
			var secondArgument = SecondArgument;
			var thirdArgument = ThirdArgument;

			#if DEBUG || INIT_ARGS_SAFE_MODE
			if(nullArgumentGuard.IsEnabled(NullArgumentGuard.RuntimeException))
			{
				if(firstArgument == Null) throw GetMissingInitArgumentsException(GetType(), typeof(TClient), typeof(TFirstArgument));
				if(secondArgument == Null) throw GetMissingInitArgumentsException(GetType(), typeof(TClient), typeof(TSecondArgument));
				if(thirdArgument == Null) throw GetMissingInitArgumentsException(GetType(), typeof(TClient), typeof(TThirdArgument));
			}
			#endif

			target = InitTarget(firstArgument, secondArgument, thirdArgument);
			Updater.InvokeAtEndOfFrame(DestroySelf);
			return target;
		}

		/// <summary>
		/// Resets the Init arguments to their default values.
		/// <para>
		/// <see cref="OnReset"/> is called when the user hits the Reset button in the Inspector's
		/// context menu or when adding the component to a GameObject the first time.
		/// <para>
		/// This function is only called in the editor in edit mode.
		/// </summary>
		/// <param name="firstArgument"> The first argument to reset. </param>
		/// <param name="secondArgument"> The second argument to reset. </param>
		/// <param name="thirdArgument"> The third argument to reset. </param>
		protected virtual void OnReset(ref TFirstArgument firstArgument, ref TSecondArgument secondArgument, ref TThirdArgument thirdArgument) { }

		/// <summary>
		/// Initializes the existing <see cref="target"/> or new instance of type <see cref="TClient"/> using the provided arguments.
		/// </summary>
		/// <param name="firstArgument"> The first argument to pass to the target's Init function. </param>
		/// <param name="secondArgument"> The second argument to pass to the target's Init function. </param>
		/// <param name="thirdArgument"> The third argument to pass to the target's Init function. </param>
		/// <returns> The existing <see cref="target"/> or new instance of type <see cref="TClient"/>. </returns>
		[NotNull]
		protected virtual TClient InitTarget(TFirstArgument firstArgument, TSecondArgument secondArgument, TThirdArgument thirdArgument)
        {
            if(target == null)
            {
                return target = gameObject.AddComponent<TClient, TFirstArgument, TSecondArgument, TThirdArgument>(firstArgument, secondArgument, thirdArgument);
            }

			if(target.gameObject != gameObject)
			{
				return target.Instantiate(firstArgument, secondArgument, thirdArgument);
            }

			if(target is MonoBehaviour<TFirstArgument, TSecondArgument, TThirdArgument> monoBehaviourT)
			{
				monoBehaviourT.InitInternal(firstArgument, secondArgument, thirdArgument);
			}
			else
			{
				target.Init(firstArgument, secondArgument, thirdArgument);
			}

			return target;
        }

		#if UNITY_EDITOR
        private void Reset()
		{
			var set = HandleReset(this, ref target, FirstArgument, SecondArgument, ThirdArgument, OnReset);
			if(!AreEqual(FirstArgument, set.firstArgument)) FirstArgument = set.firstArgument;
			if(!AreEqual(SecondArgument, set.secondArgument)) SecondArgument = set.secondArgument;
			if(!AreEqual(ThirdArgument, set.thirdArgument)) ThirdArgument = set.thirdArgument;
		}

		private void OnValidate() => OnMainThread(Validate);
		#endif

		protected virtual void Validate()
		{
			#if UNITY_EDITOR
			ValidateOnMainThread(this);
			#endif
		}

		private void Awake() => InitTarget();

		private void DestroySelf()
		{
			if(this != null)
			{
				Destroy(this);
			}
		}
	}
}