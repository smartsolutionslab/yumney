import { Component, signal } from '@angular/core';
import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { debouncedEffect } from './debounced-effect';

@Component({ template: '' })
class HostComponent {
  readonly source = signal('');
  readonly callbackInvocations: string[] = [];

  constructor() {
    debouncedEffect(this.source, 400, (value) => {
      this.callbackInvocations.push(value);
    });
  }
}

describe('debouncedEffect', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HostComponent] });
  });

  it('does not invoke the callback on the first effect run', fakeAsync(() => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    TestBed.tick();
    tick(1000);

    expect(fixture.componentInstance.callbackInvocations).toEqual([]);
  }));

  it('invokes the callback once after the debounce window elapses', fakeAsync(() => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    TestBed.tick();

    fixture.componentInstance.source.set('hello');
    TestBed.tick();
    tick(399);
    expect(fixture.componentInstance.callbackInvocations).toEqual([]);

    tick(1);
    expect(fixture.componentInstance.callbackInvocations).toEqual(['hello']);
  }));

  it('collapses successive changes into a single fire with the latest value', fakeAsync(() => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    TestBed.tick();

    fixture.componentInstance.source.set('first');
    TestBed.tick();
    tick(200);
    fixture.componentInstance.source.set('second');
    TestBed.tick();
    tick(200);
    fixture.componentInstance.source.set('third');
    TestBed.tick();
    tick(200);

    expect(fixture.componentInstance.callbackInvocations).toEqual([]);

    tick(200);
    expect(fixture.componentInstance.callbackInvocations).toEqual(['third']);
  }));

  it('cancels the pending callback when the host is destroyed', fakeAsync(() => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    TestBed.tick();

    fixture.componentInstance.source.set('hello');
    TestBed.tick();
    tick(200);

    fixture.destroy();
    tick(1000);

    expect(fixture.componentInstance.callbackInvocations).toEqual([]);
  }));
});
