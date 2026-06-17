using NUnit.Framework;
using UnityEngine;

public sealed class CannonMountTests
{
	[Test]
	public void SetAngle_ClampsToConfiguredLimits()
	{
		GameObject cannonObject = new GameObject(nameof(SetAngle_ClampsToConfiguredLimits));
		try
		{
			CannonMount mount = cannonObject.AddComponent<CannonMount>();
			mount.ConfigureAim(-25f, 40f, 5f);

			mount.SetAngle(100f);
			Assert.That(mount.CurrentAngle, Is.EqualTo(40f));

			mount.SetAngle(-100f);
			Assert.That(mount.CurrentAngle, Is.EqualTo(-25f));
		}
		finally
		{
			Object.DestroyImmediate(cannonObject);
		}
	}

	[Test]
	public void ResetAngle_RestoresConfiguredInitialAngle()
	{
		GameObject cannonObject = new GameObject(nameof(ResetAngle_RestoresConfiguredInitialAngle));
		try
		{
			CannonMount mount = cannonObject.AddComponent<CannonMount>();
			mount.ConfigureAim(-45f, 45f, 12f);
			mount.AdjustAngle(20f);

			mount.ResetAngle();

			Assert.That(mount.CurrentAngle, Is.EqualTo(12f));
		}
		finally
		{
			Object.DestroyImmediate(cannonObject);
		}
	}

	[Test]
	public void SetAngle_AppliesRotationRelativeToStageCenterDirection()
	{
		GameObject cannonObject = new GameObject(nameof(SetAngle_AppliesRotationRelativeToStageCenterDirection));
		GameObject centerObject = new GameObject("StageCenter");
		try
		{
			cannonObject.transform.position = new Vector3(0f, 5f, 0f);
			centerObject.transform.position = Vector3.zero;
			CannonMount mount = cannonObject.AddComponent<CannonMount>();
			mount.ConfigureStageCenter(centerObject.transform);
			mount.ConfigureAim(-60f, 60f, 0f);

			mount.SetAngle(30f);

			Assert.That(Mathf.DeltaAngle(cannonObject.transform.eulerAngles.z, 300f), Is.EqualTo(0f).Within(0.001f));
		}
		finally
		{
			Object.DestroyImmediate(cannonObject);
			Object.DestroyImmediate(centerObject);
		}
	}

	[Test]
	public void SetWorldDirection_UsesRightStickDirectionRelativeToStageCenter()
	{
		GameObject cannonObject = new GameObject(nameof(SetWorldDirection_UsesRightStickDirectionRelativeToStageCenter));
		GameObject centerObject = new GameObject("StageCenter");
		try
		{
			cannonObject.transform.position = new Vector3(0f, 5f, 0f);
			centerObject.transform.position = Vector3.zero;
			CannonMount mount = cannonObject.AddComponent<CannonMount>();
			mount.ConfigureStageCenter(centerObject.transform);
			mount.ConfigureAim(-60f, 60f, 0f);

			mount.SetWorldDirection(new Vector2(1f, -1f));

			Assert.That(mount.CurrentAngle, Is.EqualTo(45f).Within(0.001f));
			Assert.That(Mathf.DeltaAngle(cannonObject.transform.eulerAngles.z, 315f), Is.EqualTo(0f).Within(0.001f));
		}
		finally
		{
			Object.DestroyImmediate(cannonObject);
			Object.DestroyImmediate(centerObject);
		}
	}
}
