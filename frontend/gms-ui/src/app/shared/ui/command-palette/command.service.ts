import { Injectable, signal } from '@angular/core';
import { IconName } from '../../icon/icon';

export type CommandGroup = 'nav' | 'create' | 'actions' | 'results';

export interface CommandEntry {
  id: string;
  /** Translation key — used for registry-provided entries. */
  labelKey?: string;
  /** Literal label — used for dynamically-built entries (e.g. live search results). */
  label?: string;
  hintKey?: string;
  hint?: string;
  icon: IconName;
  group: CommandGroup;
  /** Navigate to a route… */
  route?: string;
  /** …or run an arbitrary action. */
  run?: () => void;
}

const DEFAULT_COMMANDS: CommandEntry[] = [
  { id: 'nav-hub', labelKey: 'nav.hub', hintKey: 'commandPalette.hint.workspace', icon: 'hub', route: '/hub', group: 'nav' },
  { id: 'nav-dashboard', labelKey: 'nav.dashboard', hintKey: 'commandPalette.hint.operations', icon: 'dashboard', route: '/dashboard', group: 'nav' },
  { id: 'nav-releases', labelKey: 'nav.releases', hintKey: 'commandPalette.hint.release', icon: 'release', route: '/releases', group: 'nav' },
  { id: 'nav-employees', labelKey: 'nav.employees', hintKey: 'commandPalette.hint.organization', icon: 'employees', route: '/employees', group: 'nav' },
  { id: 'nav-training', labelKey: 'nav.training', hintKey: 'commandPalette.hint.development', icon: 'training', route: '/training', group: 'nav' },
  { id: 'nav-leave', labelKey: 'nav.leave', hintKey: 'commandPalette.hint.team', icon: 'calendar', route: '/leave', group: 'nav' },
  { id: 'nav-notifications', labelKey: 'nav.notifications', hintKey: 'commandPalette.hint.notification', icon: 'bell', route: '/notifications', group: 'nav' },
  { id: 'nav-profile', labelKey: 'commandPalette.myProfile', hintKey: 'commandPalette.hint.account', icon: 'user', route: '/profile', group: 'nav' },
  { id: 'create-release', labelKey: 'commandPalette.createRelease', hintKey: 'commandPalette.hint.quickCreate', icon: 'plus', route: '/releases', group: 'create' }
];

/**
 * Command registry for the Ctrl/⌘+K palette.
 * Modules register their own nav / quick-create / action commands:
 * `inject(CommandService).register([{ id, labelKey, hintKey, icon, group, run/route }])`
 */
@Injectable({ providedIn: 'root' })
export class CommandService {
  private readonly registered = signal<CommandEntry[]>(DEFAULT_COMMANDS);
  readonly commands = this.registered.asReadonly();

  register(entries: CommandEntry[]): void {
    this.registered.update((list) => {
      const ids = new Set(list.map((c) => c.id));
      return [...list, ...entries.filter((e) => !ids.has(e.id))];
    });
  }

  unregister(ids: string[]): void {
    const drop = new Set(ids);
    this.registered.update((list) => list.filter((c) => !drop.has(c.id)));
  }
}
