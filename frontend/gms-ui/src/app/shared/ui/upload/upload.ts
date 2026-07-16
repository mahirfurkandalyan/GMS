import { Component, input, output, signal } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { GmsIcon, IconName } from '../../icon/icon';

export type UploadKind = 'pdf' | 'sql' | 'text' | 'image' | 'word' | 'excel' | 'other';

export interface UploadedFile {
  name: string;
  sizeLabel: string;
  kind: UploadKind;
}

/**
 * Reusable upload zone (architecture-only — no real storage).
 * Prepared for: drag & drop, multi upload, version upload, replace existing,
 * file validation and virus scan. Emits captured file metadata via `filesChange`.
 */
@Component({
  selector: 'gms-upload-zone',
  standalone: true,
  imports: [GmsIcon, TranslocoPipe],
  template: `
    <div class="uz" [class.uz--drag]="dragActive()"
      (dragover)="onDragOver($event)" (dragleave)="dragActive.set(false)" (drop)="onDrop($event)">
      <span class="uz__icon"><gms-icon name="document" [size]="26" /></span>
      <span class="uz__title">{{ 'upload.dragDrop' | transloco }}</span>
      <span class="uz__sub">{{ 'upload.or' | transloco }}</span>
      <label class="gms-btn gms-btn--secondary gms-btn--sm">
        <gms-icon name="plus" [size]="15" /> {{ 'upload.chooseFile' | transloco }}
        <input type="file" [multiple]="multiple()" (change)="onSelect($event)" hidden />
      </label>
      <span class="uz__hint">{{ 'upload.hint' | transloco }}</span>
    </div>

    @if (files().length) {
      <ul class="uz-list">
        @for (f of files(); track f.name; let i = $index) {
          <li class="uz-file">
            <span class="uz-file__icon"><gms-icon [name]="kindIcon(f.kind)" [size]="16" /></span>
            <span class="uz-file__main">
              <span class="uz-file__name">{{ f.name }}</span>
              <span class="uz-file__meta">{{ f.sizeLabel }}</span>
            </span>
            <span class="badge badge--success"><gms-icon name="shield" [size]="12" /> {{ 'upload.scanned' | transloco }}</span>
            <button type="button" class="uz-file__remove" (click)="remove(i)" [attr.aria-label]="'common.remove' | transloco"><gms-icon name="close" [size]="14" /></button>
          </li>
        }
      </ul>
    }
  `,
  styles: [`
    :host { display: block; }
    .uz {
      display: flex; flex-direction: column; align-items: center; gap: 6px; text-align: center;
      padding: var(--s-6) var(--s-4);
      border: 2px dashed var(--border-strong); border-radius: var(--r-lg);
      background: var(--surface-sunken); transition: border-color var(--motion-fast) var(--ease), background var(--motion-fast) var(--ease);
    }
    .uz--drag { border-color: var(--brand); background: var(--brand-subtle); }
    .uz__icon { width: 48px; height: 48px; border-radius: var(--r-md); background: var(--surface); border: 1px solid var(--border); display: flex; align-items: center; justify-content: center; color: var(--text-muted); }
    .uz__title { font-size: var(--fs-body); font-weight: 600; color: var(--text-strong); margin-top: var(--s-2); }
    .uz__sub { font-size: var(--fs-sm); color: var(--text-subtle); }
    .uz label { cursor: pointer; }
    .uz__hint { font-size: var(--fs-caption); color: var(--text-subtle); max-width: 420px; margin-top: 4px; }
    .uz-list { list-style: none; margin: var(--s-3) 0 0; padding: 0; display: flex; flex-direction: column; gap: var(--s-2); }
    .uz-file { display: flex; align-items: center; gap: var(--s-3); padding: var(--s-2) var(--s-3); border: 1px solid var(--border); border-radius: var(--r-sm); background: var(--surface); }
    .uz-file__icon { width: 30px; height: 30px; border-radius: var(--r-sm); background: var(--surface-sunken); color: var(--text-muted); display: flex; align-items: center; justify-content: center; flex-shrink: 0; }
    .uz-file__main { flex: 1; min-width: 0; display: flex; flex-direction: column; }
    .uz-file__name { font-size: var(--fs-sm); font-weight: 600; color: var(--text-strong); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .uz-file__meta { font-size: var(--fs-caption); color: var(--text-muted); }
    .uz-file__remove { width: 26px; height: 26px; border: 0; background: transparent; color: var(--text-subtle); border-radius: var(--r-sm); cursor: pointer; display: flex; align-items: center; justify-content: center; }
    .uz-file__remove:hover { background: var(--danger-bg); color: var(--danger); }
  `]
})
export class GmsUploadZone {
  readonly multiple = input(true);
  readonly filesChange = output<UploadedFile[]>();

  protected readonly files = signal<UploadedFile[]>([]);
  protected readonly dragActive = signal(false);

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.dragActive.set(true);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragActive.set(false);
    this.add(event.dataTransfer?.files);
  }

  onSelect(event: Event): void {
    this.add((event.target as HTMLInputElement).files);
  }

  remove(index: number): void {
    this.files.update((list) => list.filter((_, i) => i !== index));
    this.filesChange.emit(this.files());
  }

  private add(fileList: FileList | null | undefined): void {
    if (!fileList) return;
    const next: UploadedFile[] = [];
    for (let i = 0; i < fileList.length; i++) {
      const f = fileList.item(i);
      if (f) next.push({ name: f.name, sizeLabel: this.formatSize(f.size), kind: this.kindOf(f.name) });
    }
    this.files.update((list) => (this.multiple() ? [...list, ...next] : next.slice(-1)));
    this.filesChange.emit(this.files());
  }

  kindIcon(kind: UploadKind): IconName {
    switch (kind) {
      case 'sql': return 'server';
      case 'image': return 'grid';
      default: return 'document';
    }
  }

  private kindOf(name: string): UploadKind {
    const ext = name.split('.').pop()?.toLowerCase() ?? '';
    if (ext === 'pdf') return 'pdf';
    if (ext === 'sql') return 'sql';
    if (['txt', 'md', 'log'].includes(ext)) return 'text';
    if (['png', 'jpg', 'jpeg', 'gif', 'svg'].includes(ext)) return 'image';
    if (['doc', 'docx'].includes(ext)) return 'word';
    if (['xls', 'xlsx', 'csv'].includes(ext)) return 'excel';
    return 'other';
  }

  private formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`;
    return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
  }
}
