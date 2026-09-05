import React, { useState, useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams, useNavigate } from 'react-router-dom';
import { AdminFormLayout } from '../../../../../components/admin/AdminFormLayout';
import { Button, Field, inputClasses, RichTextEditor } from '../../../../../components/ui';
import { teamMembersApi } from '../../../../../lib/api/team-members';
import type { UpdateTeamMemberRequest } from '../../../../../lib/api/types';

const TeamMemberEditPage: React.FC = () => {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const formRef = useRef<HTMLFormElement>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [formData, setFormData] = useState<UpdateTeamMemberRequest>({
    name: '',
    position: '',
    positionEn: '',
    region: '',
    regionEn: '',
    zone: '',
    zoneEn: '',
    photo: '',
    bio: '',
    bioEn: '',
    email: '',
    order: 0,
    isActive: true
  });

  const backPath = `/admin/team-members/${id}`;

  useEffect(() => {
    const loadMember = async () => {
      if (!id) return;

      try {
        setLoading(true);
        const response = await teamMembersApi.getTeamMemberById(id);
        if (response.success && response.data) {
          const member = response.data;
          setFormData({
            name: member.name,
            position: member.position,
            positionEn: member.positionEn || '',
            region: member.region,
            regionEn: member.regionEn || '',
            zone: member.zone,
            zoneEn: member.zoneEn || '',
            photo: member.photo || '',
            bio: member.bio || '',
            bioEn: member.bioEn || '',
            email: member.email || '',
            order: member.order,
            isActive: member.isActive
          });
        } else {
          setError(t('admin.team.errorLoad'));
        }
      } catch (err) {
        console.error('Error loading team member:', err);
        setError(t('admin.team.errorLoad'));
      } finally {
        setLoading(false);
      }
    };

    loadMember();
  }, [id]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!id) return;

    setSubmitting(true);
    setError(null);

    try {
      const response = await teamMembersApi.updateTeamMember(id, formData);
      if (response.success) {
        navigate(`/admin/team-members/${id}`);
      } else {
        setError(t('admin.team.errorUpdate'));
      }
    } catch (err) {
      console.error('Error updating team member:', err);
      setError(t('admin.team.errorUpdate'));
    } finally {
      setSubmitting(false);
    }
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    const { name, value, type } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: type === 'number' ? parseInt(value) :
              type === 'checkbox' ? (e.target as HTMLInputElement).checked :
              value
    }));
  };

  if (loading) {
    return (
      <div className="flex justify-center py-12">
        <div className="h-8 w-8 animate-spin border-2 border-line border-t-green" />
      </div>
    );
  }

  return (
    <form ref={formRef} onSubmit={handleSubmit} className="min-w-0">
      <AdminFormLayout
        title={t('admin.team.editTitle')}
        backPath={backPath}
        backLabel={t('admin.common.cancel')}
        onCancel={() => navigate(backPath)}
        onSave={() => formRef.current?.requestSubmit()}
        actions={
          <Button type="submit" variant="primary" disabled={submitting}>
            {submitting ? t('admin.common.saving') : t('admin.common.saveChanges')}
          </Button>
        }
        main={
          <div>
            {error && (
              <p className="mb-6 border border-error bg-surface px-4 py-3 text-error">{error}</p>
            )}
            <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
              <div className="md:col-span-2">
                <Field label={t('admin.team.name')} htmlFor="name" required>
                  <input
                    type="text"
                    id="name"
                    name="name"
                    value={formData.name}
                    onChange={handleChange}
                    required
                    className={inputClasses}
                  />
                </Field>
              </div>

              <div className="md:col-span-2">
                <Field label={t('admin.team.positionFr')} htmlFor="position" required>
                  <input
                    type="text"
                    id="position"
                    name="position"
                    value={formData.position}
                    onChange={handleChange}
                    required
                    className={inputClasses}
                  />
                </Field>
              </div>

              <div className="md:col-span-2"><Field label={t('admin.team.positionEn')} htmlFor="positionEn"><input type="text" id="positionEn" name="positionEn" value={formData.positionEn} onChange={handleChange} className={inputClasses} /></Field></div>

              <Field label={t('admin.team.regionFr')} htmlFor="region" required>
                <input
                  type="text"
                  id="region"
                  name="region"
                  value={formData.region}
                  onChange={handleChange}
                  required
                  className={inputClasses}
                />
              </Field>

              <Field label={t('admin.team.regionEn')} htmlFor="regionEn"><input type="text" id="regionEn" name="regionEn" value={formData.regionEn} onChange={handleChange} className={inputClasses} /></Field>

              <Field label={t('admin.common.zone')} htmlFor="zone" required>
                <select
                  id="zone"
                  name="zone"
                  value={formData.zone}
                  onChange={handleChange}
                  required
                  className={`${inputClasses} cursor-pointer`}
                >
                  <option value="">{t('admin.team.selectZone')}</option>
                  <option value="Zone 1">Zone 1</option>
                  <option value="Zone 2">Zone 2</option>
                </select>
              </Field>

              <Field label={t('admin.team.zoneEn')} htmlFor="zoneEn"><input type="text" id="zoneEn" name="zoneEn" value={formData.zoneEn} onChange={handleChange} className={inputClasses} /></Field>

              <div className="md:col-span-2">
                <Field label={t('admin.common.email')} htmlFor="email">
                  <input
                    type="email"
                    id="email"
                    name="email"
                    value={formData.email}
                    onChange={handleChange}
                    className={inputClasses}
                  />
                </Field>
              </div>

              <div className="md:col-span-2">
                <Field label={t('admin.team.photoUrl')} htmlFor="photo">
                  <input
                    type="url"
                    id="photo"
                    name="photo"
                    value={formData.photo}
                    onChange={handleChange}
                    className={inputClasses}
                    placeholder="https://example.com/photo.jpg"
                  />
                </Field>
              </div>

              <div className="md:col-span-2">
                <Field label={t('admin.team.biographyFr')} htmlFor="bio">
                  <RichTextEditor
                    id="bio"
                    value={formData.bio}
                    onChange={(bio) => setFormData((current) => ({ ...current, bio }))}
                    minHeight={260}
                    label={t('admin.team.biographyFr')}
                  />
                </Field>
              </div>

              <div className="md:col-span-2"><Field label={t('admin.team.biographyEn')} htmlFor="bioEn"><RichTextEditor id="bioEn" value={formData.bioEn} onChange={(bioEn) => setFormData((current) => ({ ...current, bioEn }))} minHeight={260} label={t('admin.team.biographyEn')} /></Field></div>

              <Field label={t('admin.common.order')} htmlFor="order" required>
                <input
                  type="number"
                  id="order"
                  name="order"
                  value={formData.order}
                  onChange={handleChange}
                  required
                  min="0"
                  className={inputClasses}
                />
              </Field>

              <div className="flex items-center">
                <label htmlFor="isActive" className="flex min-h-[44px] cursor-pointer items-center gap-3">
                  <input
                    type="checkbox"
                    id="isActive"
                    name="isActive"
                    checked={formData.isActive}
                    onChange={handleChange}
                    className="h-5 w-5 rounded-control-sm border border-outline accent-green"
                  />
                  <span className="text-body-md text-ink">{t('admin.common.active')}</span>
                </label>
              </div>
            </div>
          </div>
        }
      />
    </form>
  );
};

export default TeamMemberEditPage;
