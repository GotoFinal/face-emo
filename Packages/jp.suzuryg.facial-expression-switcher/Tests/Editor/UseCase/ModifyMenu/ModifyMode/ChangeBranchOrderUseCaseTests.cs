﻿using NUnit.Framework;
using Suzuryg.FacialExpressionSwitcher.Domain;
using Suzuryg.FacialExpressionSwitcher.UseCase;
using Suzuryg.FacialExpressionSwitcher.UseCase.ModifyMenu;
using System.Collections.Generic;

namespace Suzuryg.FacialExpressionSwitcher.UseCase.ModifyMenu.ModifyMode
{
    public class MockChangeBranchOrderPresenter : IChangeBranchOrderPresenter
    {
        public ChangeBranchOrderResult Result { get; private set; }

        public event System.Action<ChangeBranchOrderResult, IMenu, string> OnCompleted;

        void IChangeBranchOrderPresenter.Complete(ChangeBranchOrderResult changeBranchOrderResult, in IMenu menu, string errorMessage)
        {
            Result = changeBranchOrderResult;
        }
    }

    public class ChangeBranchOrderUseCaseTests
    {
        [Test]
        public void Test()
        {
            UseCaseTestsInstaller useCaseTestsInstaller = new UseCaseTestsInstaller();
            useCaseTestsInstaller.Install();

            var menuRepository = useCaseTestsInstaller.Container.Resolve<IMenuRepository>();
            var menuId = UseCaseTestSetting.MenuId;
            System.Func<Menu> loadMenu = () => menuRepository.Load(menuId);

            ChangeBranchOrderUseCase changeBranchOrderUseCase = useCaseTestsInstaller.Container.Resolve<ChangeBranchOrderUseCase>();
            MockChangeBranchOrderPresenter mockChangeBranchOrderPresenter = useCaseTestsInstaller.Container.Resolve<IChangeBranchOrderPresenter>() as MockChangeBranchOrderPresenter;

            // null
            changeBranchOrderUseCase.Handle(null, "", 0, 0);
            Assert.That(mockChangeBranchOrderPresenter.Result, Is.EqualTo(ChangeBranchOrderResult.ArgumentNull));
            changeBranchOrderUseCase.Handle("", null, 0, 0);
            Assert.That(mockChangeBranchOrderPresenter.Result, Is.EqualTo(ChangeBranchOrderResult.ArgumentNull));

            // Menu is not opened
            changeBranchOrderUseCase.Handle(menuId, "", 0, 0);
            Assert.That(mockChangeBranchOrderPresenter.Result, Is.EqualTo(ChangeBranchOrderResult.MenuDoesNotExist));

            // Create menu
            CreateMenuUseCase createMenuUseCase = useCaseTestsInstaller.Container.Resolve<CreateMenuUseCase>();
            createMenuUseCase.Handle(menuId);

            // Invalid branch
            changeBranchOrderUseCase.Handle(menuId, "", 0, 0);
            Assert.That(mockChangeBranchOrderPresenter.Result, Is.EqualTo(ChangeBranchOrderResult.InvalidBranch));

            changeBranchOrderUseCase.Handle(menuId, Menu.RegisteredId, 0, 0);
            Assert.That(mockChangeBranchOrderPresenter.Result, Is.EqualTo(ChangeBranchOrderResult.InvalidBranch));

            changeBranchOrderUseCase.Handle(menuId, Menu.UnregisteredId, 0, 0);
            Assert.That(mockChangeBranchOrderPresenter.Result, Is.EqualTo(ChangeBranchOrderResult.InvalidBranch));

            // Add mode
            AddMenuItemUseCase addMenuItemUseCase = useCaseTestsInstaller.Container.Resolve<AddMenuItemUseCase>();
            addMenuItemUseCase.Handle(menuId, Menu.RegisteredId, AddMenuItemType.Mode);
            addMenuItemUseCase.Handle(menuId, Menu.RegisteredId, AddMenuItemType.Mode);

            // Add branch
            AddBranchUseCase addBranchUseCase = useCaseTestsInstaller.Container.Resolve<AddBranchUseCase>();
            addBranchUseCase.Handle(menuId, loadMenu().Registered.Order[0]);
            addBranchUseCase.Handle(menuId, loadMenu().Registered.Order[0]);
            addBranchUseCase.Handle(menuId, loadMenu().Registered.Order[0]);
            var b0 = loadMenu().Registered.GetModeAt(0).Branches[0];
            var b1 = loadMenu().Registered.GetModeAt(0).Branches[1];
            var b2 = loadMenu().Registered.GetModeAt(0).Branches[2];

            // Change branch order
            // Invalid
            changeBranchOrderUseCase.Handle(menuId, loadMenu().Registered.Order[0], -1, 0);
            Assert.That(mockChangeBranchOrderPresenter.Result, Is.EqualTo(ChangeBranchOrderResult.InvalidBranch));

            changeBranchOrderUseCase.Handle(menuId, loadMenu().Registered.Order[0], 3, 0);
            Assert.That(mockChangeBranchOrderPresenter.Result, Is.EqualTo(ChangeBranchOrderResult.InvalidBranch));

            // Success
            changeBranchOrderUseCase.Handle(menuId, loadMenu().Registered.Order[0], 2, -1);
            Assert.That(mockChangeBranchOrderPresenter.Result, Is.EqualTo(ChangeBranchOrderResult.Succeeded));
            Assert.That(loadMenu().Registered.GetModeAt(0).Branches[0], Is.SameAs(b2));
            Assert.That(loadMenu().Registered.GetModeAt(0).Branches[1], Is.SameAs(b0));
            Assert.That(loadMenu().Registered.GetModeAt(0).Branches[2], Is.SameAs(b1));

            changeBranchOrderUseCase.Handle(menuId, loadMenu().Registered.Order[0], 1, 9999);
            Assert.That(mockChangeBranchOrderPresenter.Result, Is.EqualTo(ChangeBranchOrderResult.Succeeded));
            Assert.That(loadMenu().Registered.GetModeAt(0).Branches[0], Is.SameAs(b2));
            Assert.That(loadMenu().Registered.GetModeAt(0).Branches[1], Is.SameAs(b1));
            Assert.That(loadMenu().Registered.GetModeAt(0).Branches[2], Is.SameAs(b0));

            changeBranchOrderUseCase.Handle(menuId, loadMenu().Registered.Order[0], 1, 1);
            Assert.That(mockChangeBranchOrderPresenter.Result, Is.EqualTo(ChangeBranchOrderResult.Succeeded));
            Assert.That(loadMenu().Registered.GetModeAt(0).Branches[0], Is.SameAs(b2));
            Assert.That(loadMenu().Registered.GetModeAt(0).Branches[1], Is.SameAs(b1));
            Assert.That(loadMenu().Registered.GetModeAt(0).Branches[2], Is.SameAs(b0));

            changeBranchOrderUseCase.Handle(menuId, loadMenu().Registered.Order[0], 0, 1);
            Assert.That(mockChangeBranchOrderPresenter.Result, Is.EqualTo(ChangeBranchOrderResult.Succeeded));
            Assert.That(loadMenu().Registered.GetModeAt(0).Branches[0], Is.SameAs(b1));
            Assert.That(loadMenu().Registered.GetModeAt(0).Branches[1], Is.SameAs(b2));
            Assert.That(loadMenu().Registered.GetModeAt(0).Branches[2], Is.SameAs(b0));
        }
    }
}
