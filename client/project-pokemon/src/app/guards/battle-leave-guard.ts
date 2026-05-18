import { CanDeactivateFn } from '@angular/router';
import { Battle } from '../pages/battle/battle';

export const battleLeaveGuard: CanDeactivateFn<Battle> = (component) => {
  return component.canLeaveBattle();
};
