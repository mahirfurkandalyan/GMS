import { MenuItem } from '../menu/menu';

/**
 * Standard enterprise row actions — reusable across every module's grid
 * (EBR / MES / LIMS / QMS / CAPA / Deviation / Audit …).
 * Consumers filter/extend as needed and handle values in (rowAction).
 */
export const STANDARD_ROW_ACTIONS: MenuItem[] = [
  { label: 'Aç', value: 'open', icon: 'search' },
  { label: 'Düzenle', value: 'edit', icon: 'change' },
  { label: 'Çoğalt', value: 'duplicate', icon: 'document' },
  { label: 'Bağlantıyı Kopyala', value: 'copy-link', icon: 'share' },
  { label: 'Arşivle', value: 'archive', icon: 'folder' },
  { label: 'Sil', value: 'delete', icon: 'close', tone: 'danger' }
];

/** Standard bulk actions for multi-selection. */
export const STANDARD_BULK_ACTIONS: MenuItem[] = [
  { label: 'Dışa Aktar', value: 'export', icon: 'document' },
  { label: 'Arşivle', value: 'archive', icon: 'folder' },
  { label: 'Durum Değiştir', value: 'status', icon: 'approval' },
  { label: 'Sil', value: 'delete', icon: 'close', tone: 'danger' }
];
