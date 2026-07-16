import { Component, Input } from '@angular/core';

/**
 * GMS ikon sistemi — tek, tutarlı çizgi (stroke) ikon seti.
 * Emoji kullanılmaz. currentColor ile renk alır, boyut `size` ile ayarlanır.
 */
export type IconName =
  | 'hub'
  | 'dashboard'
  | 'release'
  | 'training'
  | 'bell'
  | 'employees'
  | 'department'
  | 'team'
  | 'orgchart'
  | 'calendar'
  | 'change'
  | 'approval'
  | 'execution'
  | 'audit'
  | 'search'
  | 'plus'
  | 'check'
  | 'close'
  | 'clock'
  | 'activity'
  | 'announcement'
  | 'lock'
  | 'logout'
  | 'user'
  | 'mail'
  | 'phone'
  | 'star'
  | 'filter'
  | 'chevron-down'
  | 'chevron-left'
  | 'chevron-right'
  | 'sort'
  | 'server'
  | 'document'
  | 'folder'
  | 'sidebar'
  | 'inbox'
  | 'briefcase'
  | 'shield'
  | 'share'
  | 'pin'
  | 'star-filled'
  | 'grid'
  | 'menu';

@Component({
  selector: 'gms-icon',
  standalone: true,
  template: `
    <svg
      [attr.width]="size"
      [attr.height]="size"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      stroke-width="1.75"
      stroke-linecap="round"
      stroke-linejoin="round"
      class="gms-icon"
      aria-hidden="true"
      focusable="false">
      @switch (name) {
        @case ('hub') { <rect x="3" y="3" width="7" height="7" rx="1.5"/><rect x="14" y="3" width="7" height="7" rx="1.5"/><rect x="3" y="14" width="7" height="7" rx="1.5"/><rect x="14" y="14" width="7" height="7" rx="1.5"/> }
        @case ('dashboard') { <path d="M3 13h8V3H3zM13 21h8V3h-8zM3 21h8v-6H3z"/> }
        @case ('release') { <path d="M12 3l7 4v6c0 4-3 6.5-7 8-4-1.5-7-4-7-8V7z"/><path d="M9.5 12l1.8 1.8L15 10"/> }
        @case ('training') { <path d="M3 7l9-4 9 4-9 4z"/><path d="M7 9v5c0 1.5 2.2 3 5 3s5-1.5 5-3V9"/> }
        @case ('bell') { <path d="M6 9a6 6 0 0 1 12 0c0 5 2 6 2 6H4s2-1 2-6z"/><path d="M10.5 20a2 2 0 0 0 3 0"/> }
        @case ('inbox') { <path d="M4 13h4l1.5 3h5L16 13h4"/><path d="M4 13V6a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v7"/><path d="M4 13v4a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-4"/> }
        @case ('employees') { <circle cx="9" cy="8" r="3.2"/><path d="M3.5 20a5.5 5.5 0 0 1 11 0"/><path d="M16 5.2a3 3 0 0 1 0 5.6"/><path d="M17.5 14.5a5.5 5.5 0 0 1 3 5.5"/> }
        @case ('department') { <path d="M4 21V5a2 2 0 0 1 2-2h6a2 2 0 0 1 2 2v16"/><path d="M14 9h4a2 2 0 0 1 2 2v10"/><path d="M8 7h2M8 11h2M8 15h2"/> }
        @case ('team') { <circle cx="8" cy="9" r="2.5"/><circle cx="16" cy="9" r="2.5"/><path d="M3.5 19a4.5 4.5 0 0 1 9 0M11.5 19a4.5 4.5 0 0 1 9 0"/> }
        @case ('orgchart') { <rect x="9" y="3" width="6" height="4" rx="1"/><rect x="3" y="15" width="6" height="4" rx="1"/><rect x="15" y="15" width="6" height="4" rx="1"/><path d="M12 7v4M6 15v-2h12v2M12 11v2"/> }
        @case ('calendar') { <rect x="3.5" y="5" width="17" height="16" rx="2"/><path d="M3.5 10h17M8 3v4M16 3v4"/> }
        @case ('change') { <path d="M4 7h11l-2.5-2.5M20 17H9l2.5 2.5"/> }
        @case ('approval') { <path d="M4 12l5 5L20 6"/> }
        @case ('execution') { <path d="M6 4l12 8-12 8z"/> }
        @case ('audit') { <path d="M12 8v4l2.5 1.5"/><circle cx="12" cy="12" r="8.5"/> }
        @case ('search') { <circle cx="11" cy="11" r="7"/><path d="m21 21-4.3-4.3"/> }
        @case ('plus') { <path d="M12 5v14M5 12h14"/> }
        @case ('check') { <path d="M20 6L9 17l-5-5"/> }
        @case ('close') { <path d="M18 6 6 18M6 6l12 12"/> }
        @case ('clock') { <circle cx="12" cy="12" r="8.5"/><path d="M12 7.5V12l3 2"/> }
        @case ('activity') { <path d="M3 12h4l3 8 4-16 3 8h4"/> }
        @case ('announcement') { <path d="M4 10v4a1 1 0 0 0 1 1h2l7 4V5L7 9H5a1 1 0 0 0-1 1z"/><path d="M17.5 9a4 4 0 0 1 0 6"/> }
        @case ('lock') { <rect x="5" y="11" width="14" height="9" rx="2"/><path d="M8 11V8a4 4 0 0 1 8 0v3"/> }
        @case ('logout') { <path d="M15 4h3a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2h-3"/><path d="M10 12h10M17 9l3 3-3 3"/> }
        @case ('user') { <circle cx="12" cy="8" r="3.5"/><path d="M5 20a7 7 0 0 1 14 0"/> }
        @case ('mail') { <rect x="3" y="5" width="18" height="14" rx="2"/><path d="m3.5 7 8.5 6 8.5-6"/> }
        @case ('phone') { <path d="M6 3h3l2 5-2.5 1.5a11 11 0 0 0 5 5L15 14l5 2v3a2 2 0 0 1-2 2A16 16 0 0 1 4 5a2 2 0 0 1 2-2z"/> }
        @case ('star') { <path d="M12 4l2.4 4.9 5.4.8-3.9 3.8.9 5.4-4.8-2.5-4.8 2.5.9-5.4L4.2 9.7l5.4-.8z"/> }
        @case ('filter') { <path d="M4 5h16l-6 8v5l-4 2v-7z"/> }
        @case ('chevron-down') { <path d="m6 9 6 6 6-6"/> }
        @case ('chevron-left') { <path d="m15 6-6 6 6 6"/> }
        @case ('chevron-right') { <path d="m9 6 6 6-6 6"/> }
        @case ('sort') { <path d="M8 4v16M8 20l-3-3M8 4l3 3M16 20V4M16 4l3 3M16 20l-3-3"/> }
        @case ('server') { <rect x="3" y="4" width="18" height="7" rx="2"/><rect x="3" y="13" width="18" height="7" rx="2"/><path d="M7 7.5h.01M7 16.5h.01"/> }
        @case ('document') { <path d="M14 3H7a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V8z"/><path d="M14 3v5h5M9 13h6M9 17h6"/> }
        @case ('folder') { <path d="M3 7a2 2 0 0 1 2-2h4l2 2h8a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/> }
        @case ('sidebar') { <rect x="3" y="4" width="18" height="16" rx="2"/><path d="M9 4v16"/> }
        @case ('briefcase') { <rect x="3" y="7" width="18" height="13" rx="2"/><path d="M8 7V5a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2M3 12h18"/> }
        @case ('shield') { <path d="M12 3l7 3v5c0 4-3 7-7 9-4-2-7-5-7-9V6z"/> }
        @case ('share') { <circle cx="18" cy="5" r="2.5"/><circle cx="6" cy="12" r="2.5"/><circle cx="18" cy="19" r="2.5"/><path d="M8.2 10.8l7.6-4.4M8.2 13.2l7.6 4.4"/> }
        @case ('pin') { <path d="M12 17v5M8 3h8l-1 6 3 3H6l3-3z"/> }
        @case ('star-filled') { <path d="M12 4l2.4 4.9 5.4.8-3.9 3.8.9 5.4-4.8-2.5-4.8 2.5.9-5.4L4.2 9.7l5.4-.8z" fill="currentColor" stroke="none"/> }
        @case ('grid') { <rect x="3" y="3" width="7" height="7" rx="1.5"/><rect x="14" y="3" width="7" height="7" rx="1.5"/><rect x="3" y="14" width="7" height="7" rx="1.5"/><rect x="14" y="14" width="7" height="7" rx="1.5"/> }
        @case ('menu') { <path d="M3 6h18M3 12h18M3 18h18"/> }
      }
    </svg>
  `,
  styles: [
    `:host { display: inline-flex; line-height: 0; }
     .gms-icon { display: block; }`
  ]
})
export class GmsIcon {
  @Input() name!: IconName;
  @Input() size = 20;
}
