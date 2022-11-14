﻿using Suzuryg.FacialExpressionSwitcher.Domain;
using NUnit.Framework;

namespace Suzuryg.FacialExpressionSwitcher.UseCase.ModifyMenu
{
    public class MockModifyModePropertiesPresenter : IModifyModePropertiesPresenter
    {
        public ModifyModePropertiesResult Result { get; private set; }

        public event System.Action<ModifyModePropertiesResult, IMenu, string> OnCompleted;

        void IModifyModePropertiesPresenter.Complete(ModifyModePropertiesResult modifyModePropertiesResult, in IMenu menu, string errorMessage)
        {
            Result = modifyModePropertiesResult;
        }
    }

    public class ModifyModePropertiesUseCaseTests
    {
        [Test]
        public void Test()
        {
            // Setup
            UseCaseTestsInstaller useCaseTestsInstaller = new UseCaseTestsInstaller();
            useCaseTestsInstaller.Install();

            var menuRepository = useCaseTestsInstaller.Container.Resolve<IMenuRepository>();
            var menuId = UseCaseTestSetting.MenuId;
            System.Func<Menu> loadMenu = () => menuRepository.Load(menuId);

            ModifyModePropertiesUseCase modifyModePropertiesUseCase = useCaseTestsInstaller.Container.Resolve<ModifyModePropertiesUseCase>();
            MockModifyModePropertiesPresenter mockModifyModePropertiesPresenter = useCaseTestsInstaller.Container.Resolve<IModifyModePropertiesPresenter>() as MockModifyModePropertiesPresenter;

            // null
            modifyModePropertiesUseCase.Handle(null, "");
            Assert.That(mockModifyModePropertiesPresenter.Result, Is.EqualTo(ModifyModePropertiesResult.ArgumentNull));
            modifyModePropertiesUseCase.Handle("", null);
            Assert.That(mockModifyModePropertiesPresenter.Result, Is.EqualTo(ModifyModePropertiesResult.ArgumentNull));

            // Menu is not opened
            modifyModePropertiesUseCase.Handle(menuId, "");
            Assert.That(mockModifyModePropertiesPresenter.Result, Is.EqualTo(ModifyModePropertiesResult.MenuDoesNotExist));

            // Create Menu
            useCaseTestsInstaller.Container.Resolve<CreateMenuUseCase>().Handle(menuId);

            // Mode is not created
            modifyModePropertiesUseCase.Handle(menuId, "");
            Assert.That(mockModifyModePropertiesPresenter.Result, Is.EqualTo(ModifyModePropertiesResult.ModeIsNotContained));

            // Add mode
            AddMenuItemUseCase addMenuItemUseCase = useCaseTestsInstaller.Container.Resolve<AddMenuItemUseCase>();
            addMenuItemUseCase.Handle(menuId, Menu.RegisteredId, AddMenuItemType.Mode);
            addMenuItemUseCase.Handle(menuId, Menu.RegisteredId, AddMenuItemType.Mode);

            var mode0Id = loadMenu().Registered.Order[0];
            var mode0 = loadMenu().Registered.GetMode(mode0Id);
            Assert.That(mode0.DisplayName, Is.EqualTo("NewMode"));
            Assert.That(mode0.UseAnimationNameAsDisplayName, Is.EqualTo(true));
            Assert.That(mode0.EyeTrackingControl, Is.EqualTo(EyeTrackingControl.Tracking));
            Assert.That(mode0.MouthTrackingControl, Is.EqualTo(MouthTrackingControl.Tracking));

            var mode1Id = loadMenu().Registered.Order[1];
            var mode1 = loadMenu().Registered.GetMode(mode1Id);
            Assert.That(mode1.DisplayName, Is.EqualTo("NewMode"));
            Assert.That(mode1.UseAnimationNameAsDisplayName, Is.EqualTo(true));
            Assert.That(mode1.EyeTrackingControl, Is.EqualTo(EyeTrackingControl.Tracking));
            Assert.That(mode1.MouthTrackingControl, Is.EqualTo(MouthTrackingControl.Tracking));

            // Invalid mode
            modifyModePropertiesUseCase.Handle(menuId, "");
            Assert.That(mockModifyModePropertiesPresenter.Result, Is.EqualTo(ModifyModePropertiesResult.ModeIsNotContained));

            // Change mode properties
            modifyModePropertiesUseCase.Handle(menuId, 
                mode0Id,
                displayName: "Changed",
                useAnimationNameAsDisplayName: false,
                eyeTrackingControl: EyeTrackingControl.Animation,
                mouthTrackingControl: MouthTrackingControl.Animation);
            Assert.That(mockModifyModePropertiesPresenter.Result, Is.EqualTo(ModifyModePropertiesResult.Succeeded));

            Assert.That(mode0.DisplayName, Is.EqualTo("Changed"));
            Assert.That(mode0.UseAnimationNameAsDisplayName, Is.EqualTo(false));
            Assert.That(mode0.EyeTrackingControl, Is.EqualTo(EyeTrackingControl.Animation));
            Assert.That(mode0.MouthTrackingControl, Is.EqualTo(MouthTrackingControl.Animation));

            Assert.That(mode1.DisplayName, Is.EqualTo("NewMode"));
            Assert.That(mode1.UseAnimationNameAsDisplayName, Is.EqualTo(true));
            Assert.That(mode1.EyeTrackingControl, Is.EqualTo(EyeTrackingControl.Tracking));
            Assert.That(mode1.MouthTrackingControl, Is.EqualTo(MouthTrackingControl.Tracking));
        }
    }
}
