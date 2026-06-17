using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;

public sealed class InputManagerTests
{
	[Test]
	public void GetAssignableGamepads_KeepsTwoPhysicalPadsEvenWhenOneLooksLikeVirtualMirror()
	{
		Gamepad xinputPad = (Gamepad)InputSystem.AddDevice(new InputDeviceDescription
		{
			deviceClass = nameof(Gamepad),
			interfaceName = "XInput",
			product = "Xbox Controller"
		});
		Gamepad playstationPad = (Gamepad)InputSystem.AddDevice(new InputDeviceDescription
		{
			deviceClass = nameof(Gamepad),
			interfaceName = "HID",
			product = "Wireless Controller"
		});
		GameObject managerObject = new GameObject(nameof(GetAssignableGamepads_KeepsTwoPhysicalPadsEvenWhenOneLooksLikeVirtualMirror));
		try
		{
			InputManager manager = managerObject.AddComponent<InputManager>();

			List<Gamepad> assignable = InvokeGetAssignableGamepads(manager, 2);

			CollectionAssert.Contains(assignable, xinputPad);
			CollectionAssert.Contains(assignable, playstationPad);
			Assert.That(assignable, Has.Count.EqualTo(2));
		}
		finally
		{
			Object.DestroyImmediate(managerObject);
			InputSystem.RemoveDevice(playstationPad);
			InputSystem.RemoveDevice(xinputPad);
		}
	}

	[Test]
	public void GetAssignableGamepads_FiltersLikelyMirrorOnlyWhenThereAreMorePadsThanPlayers()
	{
		Gamepad xinputPad = (Gamepad)InputSystem.AddDevice(new InputDeviceDescription
		{
			deviceClass = nameof(Gamepad),
			interfaceName = "XInput",
			product = "Xbox Controller"
		});
		Gamepad likelyMirror = (Gamepad)InputSystem.AddDevice(new InputDeviceDescription
		{
			deviceClass = nameof(Gamepad),
			interfaceName = "HID",
			product = "Wireless Controller"
		});
		GameObject managerObject = new GameObject(nameof(GetAssignableGamepads_FiltersLikelyMirrorOnlyWhenThereAreMorePadsThanPlayers));
		try
		{
			InputManager manager = managerObject.AddComponent<InputManager>();

			List<Gamepad> assignable = InvokeGetAssignableGamepads(manager, 1);

			CollectionAssert.Contains(assignable, xinputPad);
			CollectionAssert.DoesNotContain(assignable, likelyMirror);
			Assert.That(assignable, Has.Count.EqualTo(1));
		}
		finally
		{
			Object.DestroyImmediate(managerObject);
			InputSystem.RemoveDevice(likelyMirror);
			InputSystem.RemoveDevice(xinputPad);
		}
	}

	[Test]
	public void GetAssignableGameDevices_DoesNotReAddFilteredRecentGamepadMirror()
	{
		Gamepad xinputPad = (Gamepad)InputSystem.AddDevice(new InputDeviceDescription
		{
			deviceClass = nameof(Gamepad),
			interfaceName = "XInput",
			product = "Xbox Controller"
		});
		Gamepad likelyMirror = (Gamepad)InputSystem.AddDevice(new InputDeviceDescription
		{
			deviceClass = nameof(Gamepad),
			interfaceName = "HID",
			product = "Wireless Controller"
		});
		GameObject managerObject = new GameObject(nameof(GetAssignableGameDevices_DoesNotReAddFilteredRecentGamepadMirror));
		try
		{
			InputManager manager = managerObject.AddComponent<InputManager>();
			AddRecentlyActiveDevice(manager, likelyMirror);

			List<InputDevice> assignable = InvokeGetAssignableGameDevices(manager, 1);

			CollectionAssert.Contains(assignable, xinputPad);
			CollectionAssert.DoesNotContain(assignable, likelyMirror);
			Assert.That(assignable, Has.Count.EqualTo(1));
		}
		finally
		{
			Object.DestroyImmediate(managerObject);
			InputSystem.RemoveDevice(likelyMirror);
			InputSystem.RemoveDevice(xinputPad);
		}
	}

	[Test]
	public void GetAssignableGameDevices_FiltersLikelyJoystickMirrorOfGamepad()
	{
		Gamepad xinputPad = (Gamepad)InputSystem.AddDevice(new InputDeviceDescription
		{
			deviceClass = nameof(Gamepad),
			interfaceName = "XInput",
			product = "Xbox Controller"
		});
		Joystick likelyMirror = (Joystick)InputSystem.AddDevice(new InputDeviceDescription
		{
			deviceClass = nameof(Joystick),
			interfaceName = "HID",
			manufacturer = "XInput Wrapper",
			product = "Virtual Controller"
		});
		GameObject managerObject = new GameObject(nameof(GetAssignableGameDevices_FiltersLikelyJoystickMirrorOfGamepad));
		try
		{
			InputManager manager = managerObject.AddComponent<InputManager>();

			List<InputDevice> assignable = InvokeGetAssignableGameDevices(manager, 2);

			CollectionAssert.Contains(assignable, xinputPad);
			CollectionAssert.DoesNotContain(assignable, likelyMirror);
			Assert.That(assignable, Has.Count.EqualTo(1));
		}
		finally
		{
			Object.DestroyImmediate(managerObject);
			InputSystem.RemoveDevice(likelyMirror);
			InputSystem.RemoveDevice(xinputPad);
		}
	}

	[Test]
	public void GetAssignableGameDevices_KeepsGenericJoystickAsSecondController()
	{
		Gamepad xinputPad = (Gamepad)InputSystem.AddDevice(new InputDeviceDescription
		{
			deviceClass = nameof(Gamepad),
			interfaceName = "XInput",
			product = "Xbox Controller"
		});
		Joystick genericJoystick = (Joystick)InputSystem.AddDevice(new InputDeviceDescription
		{
			deviceClass = nameof(Joystick),
			interfaceName = "HID",
			product = "Generic USB Gamepad"
		});
		GameObject managerObject = new GameObject(nameof(GetAssignableGameDevices_KeepsGenericJoystickAsSecondController));
		try
		{
			InputManager manager = managerObject.AddComponent<InputManager>();

			List<InputDevice> assignable = InvokeGetAssignableGameDevices(manager, 2);

			CollectionAssert.Contains(assignable, xinputPad);
			CollectionAssert.Contains(assignable, genericJoystick);
			Assert.That(assignable, Has.Count.EqualTo(2));
		}
		finally
		{
			Object.DestroyImmediate(managerObject);
			InputSystem.RemoveDevice(genericJoystick);
			InputSystem.RemoveDevice(xinputPad);
		}
	}

	private static List<Gamepad> InvokeGetAssignableGamepads(InputManager manager, int requiredPlayerCount)
	{
		MethodInfo method = typeof(InputManager).GetMethod(
			"GetAssignableGamepads",
			BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.That(method, Is.Not.Null);
		return (List<Gamepad>)method.Invoke(manager, new object[] { requiredPlayerCount });
	}

	private static List<InputDevice> InvokeGetAssignableGameDevices(InputManager manager, int requiredPlayerCount)
	{
		MethodInfo method = typeof(InputManager).GetMethod(
			"GetAssignableGameDevices",
			BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.That(method, Is.Not.Null);
		return (List<InputDevice>)method.Invoke(manager, new object[] { requiredPlayerCount });
	}

	private static void AddRecentlyActiveDevice(InputManager manager, InputDevice device)
	{
		FieldInfo field = typeof(InputManager).GetField(
			"recentlyActiveGameDevices",
			BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.That(field, Is.Not.Null);
		List<InputDevice> devices = (List<InputDevice>)field.GetValue(manager);
		devices.Add(device);
	}
}
