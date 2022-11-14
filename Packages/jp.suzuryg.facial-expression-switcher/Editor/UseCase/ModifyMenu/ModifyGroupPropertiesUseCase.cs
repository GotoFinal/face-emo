﻿using Suzuryg.FacialExpressionSwitcher.Domain;
using System;

namespace Suzuryg.FacialExpressionSwitcher.UseCase.ModifyMenu
{
    public interface IModifyGroupPropertiesUseCase
    {
        void Handle(
            string menuId,
            string groupId,
            string displayName = null);
    }

    public interface IModifyGroupPropertiesPresenter
    {
        event Action<ModifyGroupPropertiesResult, IMenu, string> OnCompleted;

        void Complete(ModifyGroupPropertiesResult modifyGroupPropertiesResult, in IMenu menu, string errorMessage = "");
    }

    public enum ModifyGroupPropertiesResult
    {
        Succeeded,
        MenuDoesNotExist,
        GroupIsNotContained,
        ArgumentNull,
        Error,
    }

    public class ModifyGroupPropertiesPresenter : IModifyGroupPropertiesPresenter
    {
        public event Action<ModifyGroupPropertiesResult, IMenu, string> OnCompleted;

        public void Complete(ModifyGroupPropertiesResult modifyGroupPropertiesResult, in IMenu menu, string errorMessage = "")
        {
            OnCompleted(modifyGroupPropertiesResult, menu, errorMessage);
        }
    }

    public class ModifyGroupPropertiesUseCase : IModifyGroupPropertiesUseCase
    {
        IMenuRepository _menuRepository;
        IModifyGroupPropertiesPresenter _modifyGroupPropertiesPresenter;

        public ModifyGroupPropertiesUseCase(IMenuRepository menuRepository, IModifyGroupPropertiesPresenter modifyGroupPropertiesPresenter)
        {
            _menuRepository = menuRepository;
            _modifyGroupPropertiesPresenter = modifyGroupPropertiesPresenter;
        }

        public void Handle(
            string menuId,
            string groupId,
            string displayName = null)
        {
            try
            {
                if (menuId is null || groupId is null)
                {
                    _modifyGroupPropertiesPresenter.Complete(ModifyGroupPropertiesResult.ArgumentNull, null);
                    return;
                }

                if (!_menuRepository.Exists(menuId))
                {
                    _modifyGroupPropertiesPresenter.Complete(ModifyGroupPropertiesResult.MenuDoesNotExist, null);
                    return;
                }

                var menu = _menuRepository.Load(menuId);

                if (!menu.ContainsGroup(groupId))
                {
                    _modifyGroupPropertiesPresenter.Complete(ModifyGroupPropertiesResult.GroupIsNotContained, menu);
                    return;
                }

                menu.ModifyGroupProperties(groupId, displayName);

                _menuRepository.Save(menuId, menu);
                _modifyGroupPropertiesPresenter.Complete(ModifyGroupPropertiesResult.Succeeded, menu);
            }
            catch (Exception ex)
            {
                _modifyGroupPropertiesPresenter.Complete(ModifyGroupPropertiesResult.Error, null, ex.ToString());
            }
        }
    }
}
