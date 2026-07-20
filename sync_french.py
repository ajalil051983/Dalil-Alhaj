import json

file_path = "AIAudioGenerationFromText/categories.json"

with open(file_path, 'r', encoding='utf-8') as f:
    data = json.load(f)

for cat in data:
    if cat.get('id') == 1:
        for subcat in cat.get('subcategories', []):
            if subcat.get('id') == 102:
                # Update French to comprehensive 7-section pre-flight guide
                subcat['contentFr'] = '<h3>Préparation avant le vol vers le Hajj</h3><h4>1. Documents requis</h4><ul><li><strong>Passeport :</strong> Valide pour au moins 6 mois. Conservez une photocopie.</li><li><strong>Visa du Hajj :</strong> Vérifiez les conditions auprès d\'une agence fiable.</li><li><strong>Carte de pèlerin :</strong> Obtenez-la de l\'agence et gardez-la en sécurité.</li><li><strong>Certificats de vaccination :</strong> Vaccins requis (fièvre jaune, méningite) avec preuves.</li></ul><h4>2. Santé et préparation médicale</h4><ul><li><strong>Examen médical complet :</strong> Consultez notamment si vous avez des conditions chroniques.</li><li><strong>Médicaments :</strong> Apportez dans l\'emballage d\'origine avec ordonnance et liste anglaise.</li><li><strong>Fournitures :</strong> Pansements, onguents, vitamines, analgésiques, remèdes.</li><li><strong>Condition physique :</strong> Exercice léger pour préparer aux longues marches.</li></ul><h4>3. Vêtements et bagages intelligents</h4><ul><li><strong>Vêtements appropriés :</strong> Coton léger pour climat chaud et sec.</li><li><strong>Chaussures confortables :</strong> Faciles à enlever pour tawaf/sa\'i. Plusieurs paires.</li><li><strong>Chaussettes en coton :</strong> Quantité suffisante pour l\'absorption.</li><li><strong>Sous-vêtements :</strong> Quantité adéquate.</li><li><strong>Bagage à main :</strong> Téléphone, médicaments, documents, argent, passeport.</li><li><strong>Bagages enregistrés :</strong> Ne pas dépasser 23 kg par bagage.</li></ul><h4>4. Hygiène personnelle et soins</h4><ul><li><strong>Articles de toilette :</strong> Brosse, dentifrice, shampooing, savon liquide, lingettes, mouchoirs.</li><li><strong>Écran solaire puissant :</strong> SPF 50+ contre le soleil intense.</li><li><strong>Hydratant et onguent :</strong> Pour peau sèche et petites blessures.</li><li><strong>Produits féminins :</strong> Les femmes apportent les produits nécessaires.</li></ul><h4>5. Argent et arrangements financiers</h4><ul><li><strong>Devises :</strong> Riyals saoudiens et devise de votre pays.</li><li><strong>Cartes :</strong> Crédit et débit; avisez votre banque.</li><li><strong>Budget planifié :</strong> Achats, cadeaux, imprévus, donations.</li></ul><h4>6. Transport et logistique</h4><ul><li><strong>Billets et réservations :</strong> Confirmez vols/hôtels. Numéros de confirmation.</li><li><strong>Transport aéroport :</strong> Arrangez via votre agence.</li><li><strong>Téléphone :</strong> Carte SIM saoudienne ou itinérance internationale.</li><li><strong>Assurance complète :</strong> Couverture incluant évacuation médicale.</li></ul><h4>7. Préparation spirituelle et mentale</h4><ul><li><strong>Intention pure :</strong> Renouvelez sincèrement l\'intention pour accomplir le Hajj.</li><li><strong>Invocation :</strong> Au nom d\'Allah, je me confie à Allah, pas de force qu\'en Allah.</li><li><strong>Étudier les rituels :</strong> Lisez sur le Hajj pour vous préparer.</li><li><strong>Confiance :</strong> Confiez-vous à Allah; Il facilitera.</li></ul>'
                print("✓ Updated subcategory 102 contentFr")

with open(file_path, 'w', encoding='utf-8') as f:
    json.dump(data, f, ensure_ascii=False, indent=2)

print("✓ All three languages now aligned")
