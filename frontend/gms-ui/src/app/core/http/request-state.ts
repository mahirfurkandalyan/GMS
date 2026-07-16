import { signal, computed, Signal, WritableSignal } from '@angular/core';
import { ApiError } from '../api-error';

/**
 * Shared request-state primitive (established in the auth sprint, generalized here). Every data page
 * moves through: idle → loading → (loaded | empty | error). Actions add a separate `submitting` flag.
 * Keeping this in one place means list/detail/task pages render consistent loading/empty/error UI.
 */
export type RequestPhase = 'idle' | 'loading' | 'loaded' | 'empty' | 'error';

export interface RequestState<T> {
  phase: WritableSignal<RequestPhase>;
  data: WritableSignal<T | null>;
  error: WritableSignal<ApiError | null>;
  readonly isLoading: Signal<boolean>;
  readonly isError: Signal<boolean>;
  readonly isEmpty: Signal<boolean>;
  start(): void;
  succeed(data: T, empty?: boolean): void;
  fail(err: ApiError): void;
  reset(): void;
}

export function createRequestState<T>(): RequestState<T> {
  const phase = signal<RequestPhase>('idle');
  const data = signal<T | null>(null);
  const error = signal<ApiError | null>(null);
  return {
    phase, data, error,
    isLoading: computed(() => phase() === 'loading'),
    isError: computed(() => phase() === 'error'),
    isEmpty: computed(() => phase() === 'empty'),
    start() { phase.set('loading'); error.set(null); },
    succeed(value: T, empty = false) { data.set(value); phase.set(empty ? 'empty' : 'loaded'); },
    fail(err: ApiError) { error.set(err); phase.set('error'); },
    reset() { phase.set('idle'); data.set(null); error.set(null); }
  };
}
