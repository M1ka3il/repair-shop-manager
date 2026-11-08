import { actionLabel, lifecycleSteps } from './lifecycle';

const states = (status: Parameters<typeof lifecycleSteps>[0]) =>
  lifecycleSteps(status).map((s) => `${s.status}:${s.state}`);

describe('lifecycleSteps', () => {
  it('walks the main path', () => {
    expect(states('Completed')).toEqual([
      'Received:done',
      'InProgress:done',
      'Completed:current',
      'Invoiced:todo',
    ]);
  });

  it('shows waiting for parts as a note on "In Arbeit", not as a step of its own', () => {
    const steps = lifecycleSteps('WaitingForParts');

    expect(steps.map((s) => s.status)).not.toContain('WaitingForParts');
    expect(steps[1]).toMatchObject({
      status: 'InProgress',
      state: 'current',
      note: 'Wartet auf Teil',
    });
  });

  it('marks everything done but the last step for an invoiced order', () => {
    expect(states('Invoiced').at(-1)).toBe('Invoiced:current');
    expect(lifecycleSteps('Invoiced').every((s) => s.note === null)).toBe(true);
  });
});

describe('actionLabel', () => {
  it('names the action, depending on where the order comes from', () => {
    expect(actionLabel('Received', 'InProgress')).toBe('Arbeit beginnen');
    expect(actionLabel('WaitingForParts', 'InProgress')).toBe('Teil ist da');
    expect(actionLabel('InProgress', 'Completed')).toBe('Fertig melden');
    expect(actionLabel('Completed', 'Invoiced')).toBe('Abrechnen');
  });
});
