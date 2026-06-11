'use client';
import { useOrderTracking } from './useOrderTracking';

export default function StatusStepper({ orderId }: { orderId: string }) {
  const { currentStatus, steps } = useOrderTracking(orderId);

  const currentIndex = steps.findIndex((s) => s.key === currentStatus);

  return (
    <div className="flex flex-col gap-0">
      {steps.map((step, i) => {
        const isCompleted = i < currentIndex;
        const isCurrent = i === currentIndex;

        return (
          <div key={step.key} className="flex items-start gap-4">
            <div className="flex flex-col items-center">
              <div
                className={`flex h-8 w-8 items-center justify-center rounded-full border-2 text-sm font-bold transition-colors ${
                  isCompleted
                    ? 'border-orange-500 bg-orange-500 text-white'
                    : isCurrent
                    ? 'border-orange-500 bg-white text-orange-500'
                    : 'border-gray-200 bg-white text-gray-300'
                }`}
              >
                {isCompleted ? '✓' : i + 1}
              </div>
              {i < steps.length - 1 && (
                <div
                  className={`mt-1 h-8 w-0.5 ${
                    isCompleted ? 'bg-orange-500' : 'bg-gray-200'
                  }`}
                />
              )}
            </div>
            <div className="pb-6 pt-1">
              <p
                className={`text-sm font-medium ${
                  isCurrent ? 'text-orange-500' : isCompleted ? 'text-gray-700' : 'text-gray-400'
                }`}
              >
                {step.label}
              </p>
            </div>
          </div>
        );
      })}
    </div>
  );
}
