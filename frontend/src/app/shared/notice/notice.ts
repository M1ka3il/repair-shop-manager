import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export interface Notice {
  readonly kind: 'success' | 'error';
  readonly title: string;
  readonly detail: string;
}

/**
 * Feedback after a user action. Errors are announced immediately (role=alert),
 * confirmations politely (role=status).
 */
@Component({
  selector: 'app-notice',
  template: `
    @if (notice(); as n) {
      <div
        class="notice"
        [class.notice-error]="n.kind === 'error'"
        [attr.role]="n.kind === 'error' ? 'alert' : 'status'"
      >
        <strong>{{ n.title }}</strong>
        <span>{{ n.detail }}</span>
      </div>
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NoticeView {
  readonly notice = input<Notice | null>(null);
}
