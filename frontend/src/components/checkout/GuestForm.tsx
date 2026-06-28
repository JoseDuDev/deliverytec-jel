'use client';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';

const schema = z.object({
  name: z.string().min(3, 'Nome obrigatório'),
  phone: z.string().min(10, 'Telefone inválido').max(11),
  cpf: z.string().length(11, 'CPF deve ter 11 dígitos'),
  street: z.string().min(3, 'Rua obrigatória'),
  number: z.string().min(1, 'Número obrigatório'),
  neighborhood: z.string().min(2, 'Bairro obrigatório'),
  city: z.string().min(2, 'Cidade obrigatória'),
  complement: z.string().optional(),
  note: z.string().max(200).optional(),
});

export type CheckoutFormData = z.infer<typeof schema>;

function Field({
  id,
  label,
  placeholder,
  type = 'text',
  error,
  registration,
}: {
  id: string;
  label: string;
  placeholder: string;
  type?: string;
  error?: string;
  registration: object;
}) {
  return (
    <div className="flex flex-col gap-1.5">
      <Label htmlFor={id}>{label}</Label>
      <Input id={id} type={type} placeholder={placeholder} {...registration} />
      {error && <p className="text-xs text-destructive">{error}</p>}
    </div>
  );
}

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

      <Field id="name" label="Nome completo" placeholder="João Silva"
        error={errors.name?.message} registration={register('name')} />
      <Field id="phone" label="Telefone (só números)" placeholder="11999999999"
        error={errors.phone?.message} registration={register('phone')} />
      <Field id="cpf" label="CPF (só números)" placeholder="00000000000"
        error={errors.cpf?.message} registration={register('cpf')} />

      <h2 className="mt-2 text-lg font-bold">Endereço de entrega</h2>

      <Field id="street" label="Rua" placeholder="Rua das Flores"
        error={errors.street?.message} registration={register('street')} />
      <Field id="number" label="Número" placeholder="123"
        error={errors.number?.message} registration={register('number')} />
      <Field id="complement" label="Complemento (opcional)" placeholder="Apto 4"
        registration={register('complement')} />
      <Field id="neighborhood" label="Bairro" placeholder="Centro"
        error={errors.neighborhood?.message} registration={register('neighborhood')} />
      <Field id="city" label="Cidade" placeholder="São Paulo"
        error={errors.city?.message} registration={register('city')} />

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="note">Observações (opcional)</Label>
        <Textarea
          id="note"
          placeholder="Ex: sem cebola, não toque a campainha..."
          rows={2}
          {...register('note')}
        />
        {errors.note && <p className="text-xs text-destructive">{errors.note.message}</p>}
      </div>

      <Button
        type="submit"
        disabled={loading}
        className="mt-4 rounded-full bg-orange-500 hover:bg-orange-600 text-white py-6 text-base font-semibold"
      >
        {loading ? 'Gerando PIX...' : 'Gerar PIX'}
      </Button>
    </form>
  );
}
