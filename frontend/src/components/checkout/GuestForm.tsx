'use client';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';

const schema = z.object({
  name: z.string().min(3, 'Nome obrigatório'),
  phone: z.string().min(10, 'Telefone inválido').max(11),
  cpf: z.string().length(11, 'CPF deve ter 11 dígitos'),
  street: z.string().min(3, 'Rua obrigatória'),
  number: z.string().min(1, 'Número obrigatório'),
  neighborhood: z.string().min(2, 'Bairro obrigatório'),
  city: z.string().min(2, 'Cidade obrigatória'),
  complement: z.string().optional(),
});

export type CheckoutFormData = z.infer<typeof schema>;

export default function GuestForm({
  onSubmit,
  loading,
}: {
  onSubmit: (data: CheckoutFormData) => void;
  loading: boolean;
}) {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<CheckoutFormData>({ resolver: zodResolver(schema) });

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
      <h2 className="text-lg font-bold">Seus dados</h2>

      {[
        { field: 'name', label: 'Nome completo', placeholder: 'João Silva' },
        { field: 'phone', label: 'Telefone (só números)', placeholder: '11999999999' },
        { field: 'cpf', label: 'CPF (só números)', placeholder: '00000000000' },
      ].map(({ field, label, placeholder }) => (
        <div key={field}>
          <label className="mb-1 block text-sm font-medium text-gray-700">{label}</label>
          <input
            {...register(field as keyof CheckoutFormData)}
            placeholder={placeholder}
            className="w-full rounded-lg border px-4 py-2 focus:outline-none focus:ring-2 focus:ring-orange-400"
          />
          {errors[field as keyof CheckoutFormData] && (
            <p className="mt-1 text-xs text-red-500">
              {errors[field as keyof CheckoutFormData]?.message}
            </p>
          )}
        </div>
      ))}

      <h2 className="mt-2 text-lg font-bold">Endereço de entrega</h2>

      {[
        { field: 'street', label: 'Rua', placeholder: 'Rua das Flores' },
        { field: 'number', label: 'Número', placeholder: '123' },
        { field: 'complement', label: 'Complemento (opcional)', placeholder: 'Apto 4' },
        { field: 'neighborhood', label: 'Bairro', placeholder: 'Centro' },
        { field: 'city', label: 'Cidade', placeholder: 'São Paulo' },
      ].map(({ field, label, placeholder }) => (
        <div key={field}>
          <label className="mb-1 block text-sm font-medium text-gray-700">{label}</label>
          <input
            {...register(field as keyof CheckoutFormData)}
            placeholder={placeholder}
            className="w-full rounded-lg border px-4 py-2 focus:outline-none focus:ring-2 focus:ring-orange-400"
          />
          {errors[field as keyof CheckoutFormData] && (
            <p className="mt-1 text-xs text-red-500">
              {errors[field as keyof CheckoutFormData]?.message}
            </p>
          )}
        </div>
      ))}

      <button
        type="submit"
        disabled={loading}
        className="mt-4 w-full rounded-full bg-orange-500 py-3 font-semibold text-white disabled:opacity-60 hover:bg-orange-600"
      >
        {loading ? 'Gerando PIX...' : 'Gerar PIX'}
      </button>
    </form>
  );
}
