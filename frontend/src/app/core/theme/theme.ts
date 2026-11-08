import { DOCUMENT } from '@angular/common';
import { Injectable, effect, inject, signal } from '@angular/core';

export type ThemeChoice = 'system' | 'light' | 'dark';

/** Must match the early script in index.html, which applies it before Angular starts. */
export const THEME_STORAGE_KEY = 'repair-shop-theme';

const ORDER: readonly ThemeChoice[] = ['system', 'light', 'dark'];

const LABELS: Record<ThemeChoice, string> = {
  system: 'System',
  light: 'Hell',
  dark: 'Dunkel',
};

/**
 * Light, dark, or whatever the OS prefers. The choice is a `data-theme`
 * attribute on <html>; styles.css maps it (or its absence) to the colour
 * tokens. Stored per browser — a convenience, so storage failures (private
 * mode, blocked site data) silently fall back to "system".
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly root = inject(DOCUMENT).documentElement;

  readonly choice = signal<ThemeChoice>(readStoredChoice());

  constructor() {
    effect(() => {
      const choice = this.choice();
      if (choice === 'system') {
        delete this.root.dataset['theme'];
      } else {
        this.root.dataset['theme'] = choice;
      }
      writeStoredChoice(choice);
    });
  }

  /** System → Hell → Dunkel → System. */
  cycle(): void {
    const next = ORDER[(ORDER.indexOf(this.choice()) + 1) % ORDER.length];
    this.choice.set(next);
  }

  label(choice: ThemeChoice = this.choice()): string {
    return LABELS[choice];
  }

  nextLabel(): string {
    return LABELS[ORDER[(ORDER.indexOf(this.choice()) + 1) % ORDER.length]];
  }
}

function readStoredChoice(): ThemeChoice {
  try {
    const stored = localStorage.getItem(THEME_STORAGE_KEY);
    return stored === 'light' || stored === 'dark' ? stored : 'system';
  } catch {
    return 'system';
  }
}

function writeStoredChoice(choice: ThemeChoice): void {
  try {
    if (choice === 'system') {
      localStorage.removeItem(THEME_STORAGE_KEY);
    } else {
      localStorage.setItem(THEME_STORAGE_KEY, choice);
    }
  } catch {
    // Storage unavailable: the choice still applies for this page view.
  }
}
