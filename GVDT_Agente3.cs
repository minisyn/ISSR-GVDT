using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GVDT_Agente3 : ISSR_Agent {
    private Vector3 ps;

    public override void Start()
    {
        Debug.LogFormat("{0}: comienza", Myself.Name);
    }

    public override IEnumerator Update()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);
            // En este punto generaremos un evento de tiempo
            current_event = ISSREventType.onTickElapsed;
            current_state = AgentStateMachine();
        }
    }

    /*******************************************************
     *                  ESTADOS DEL AGENTE                 *
     *******************************************************/

    ISSRState AgentStateMachine() // Función principal de máquina de estados
    {
        ISSRState next_state = current_state; // estado de salida, en principio igual
        switch (current_state) // Según el estado
        {
            case ISSRState.Idle:
                next_state = SF_Idle();
                break;
            case ISSRState.GoingToGripSmallStone:
                next_state = SF_GoingToGripSmallStone();
            break;
                case ISSRState.GoingToGoalWithSmallStone:
                next_state = SF_GoingToGoalWithSmallStone();
            break;
            case ISSRState.AvoidingObstacle:
                next_state = SF_AvoidingObstacle();
                break;
            case ISSRState.End:
                break;
            case ISSRState.Error:
                break;
            default:
                    Debug.LogErrorFormat("{0}: estado {1} no considerado", Myself.Name, current_state);
                    break;
        }
        if (current_state != next_state) // Si ha cambiado el estado
        {
            Debug.LogWarningFormat("{0}: Estado '{1}'-->'{2}' por evento '{3}'", Myself.Name, current_state, next_state, current_event);
        }
        return next_state;
    }

    ISSRState SF_Idle()
    {
        ISSRState next_state = current_state;
        switch (current_event) // Según el evento
        {
            case ISSREventType.onTickElapsed:
                // coger de lista objeto más cercano a agente
                // que sea de tipo SmallStone, se convierte en el objeto de interés:
                focus_object = ISSRHelp.GetCloserToMeObjectInList(this, Valid_Small_Stones, ISSR_Type.SmallStone); 
                
                if (focus_object != null) // Si hay alguno (focus_object está definido)
                {
                    next_state = ProcessStone();
                }
                else // focus_object es null, no hay más piedras disponibles
                {
                    Debug.LogFormat("{0}: no conozco más piedras pequeñas", Myself.Name);
                    next_state = ISSRState.End; // Fin del proceso
                }
                break;
            default:
                Debug.LogWarningFormat("{0}: Evento '{1}' no considerado en estado '{2}'", Myself.Name, current_event, current_state);
                break;
        }
        return next_state;
    }

    private ISSRState ProcessStone()
    {
        ISSRState next_state;
        if (oiSensable(focus_object))
        {
            Debug.LogFormat("{0}: '{1}' visible, trato de cogerla", Myself.Name,
            focus_object.Name);
            acGripObject(focus_object); // intento agarrar esa piedra pequeña cercana
        }
        else
        {
            Debug.LogFormat("{0}: '{1}' fuera de la vista, voy a su última posición",
            Myself.Name, focus_object.Name);
            acGotoLocation(oiLastLocation(focus_object));
        }
        if (acCheckError()) // si hay error en la acción:
        {
            next_state = ISSRState.Error; // Señala error
        }
        else
        { // En caso de que no haya error:
            next_state = ISSRState.GoingToGripSmallStone; // cambio a estado siguiente
        }

        return next_state;
    }

    ISSRState SF_GoingToGripSmallStone()
    {
        ISSRState next_state = current_state;
        switch (current_event) // Según el evento
        {
            case ISSREventType.onGripSuccess: // Encuentra la piedra y la coge
                // Eliminamos la piedra agarrada de la lista de piedras disponibles
                Valid_Small_Stones.Remove(focus_object);

                // Pide la acción de ir a la meta
                acGotoLocation(iMyGoalLocation());
                if (acCheckError()) // Si ha habido algún error en la acción
                {
                    next_state = ISSRState.Error; // Señala error
                }
                else
                { // En caso de que no haya error:
                    next_state = ISSRState.GoingToGoalWithSmallStone; // cambio a estado siguiente
                }
                break;
            case ISSREventType.onEnterSensingArea:
                if (object_just_seen.Equals(focus_object)) // veo justo la piedra que 'recuerdo'
                {
                    acGripObject(focus_object); // intento agarrar esa piedra pequeña
                    if (acCheckError()) // si hay error en la acción:
                    {
                        next_state = ISSRState.Error; // Señala Error
                    }
                }
                break;

            case ISSREventType.onCollision:
                if (colliding_object.Equals(focus_object))
                {
                    next_state = ProcessStone();
                }
                else
                {
                    next_state = ProcessCollision();
                }
                break;

            default:
                Debug.LogWarningFormat("{0}: Evento '{1}' no considerado en estado '{2}'", Myself.Name, current_event, current_state);
                break;
        }
        return next_state;
    }
    ISSRState SF_GoingToGoalWithSmallStone()
    {
        ISSRState next_state = current_state;
        switch (current_event) // Según el evento
        {

            case ISSREventType.onGObjectScored:

                // Se ha metido la piedra en la meta
                Debug.LogFormat("{0}: Piedra {1} metida en la meta.", Myself.Name, focus_object.Name);

                // Pasamos al estado Idle
                next_state = ISSRState.Idle;
                break;
            case ISSREventType.onCollision:
                next_state = ProcessCollision();
                break;
            case ISSREventType.onGObjectCollision:
                next_state = ProcessCollision();
                break;

            default:
                Debug.LogWarningFormat("{0}: Evento '{1}' no considerado en estado '{2}'", Myself.Name, current_event, current_state);
                break;
        }
        return next_state;
    }

    ISSRState SF_AvoidingObstacle()
    {
        ISSRState next_state = current_state;
        switch (current_event) // Según el evento
        {
            case ISSREventType.onCollision:
                next_state = ProcessCollision();
                break;
            case ISSREventType.onGObjectCollision:
                next_state = ProcessCollision();
                break;
            case ISSREventType.onGObjectScored:
                next_state = ISSRState.Idle;
                break;
            case ISSREventType.onDestArrived:
                next_state = ResumeAfterCollision();
                break;

            default:
                Debug.LogWarningFormat("{0} Evento '{1}' no considerado en estado '{2}'", Myself.Name, current_event, current_state);
                break;
        }
        return next_state;

    }

    /*******************************************************
     *               FIN ESTADOS DEL AGENTE                *
     *******************************************************/

    public override void onGripSuccess(ISSR_Object obj_gripped) //Objeto agarrado por agente
    { // Acabo de agarrar un objeto: acGripObject() completado con éxito
        Debug.LogFormat("{0}: agarra '{1}'", Myself.Name, obj_gripped.Name);
        current_state = AgentStateMachine(); // Llamar a máquina de estados
    }

    public override void onGObjectScored(ISSR_Object stone_that_scored)
    { // Acabo de puntuar/ meter una piedra en la meta
        Debug.LogFormat("{0}: piedra '{1}' metida en meta", Myself.Name, stone_that_scored.Name);
        current_state = AgentStateMachine(); // Llamar a máquina de estados
    }

    public override void onCollision(ISSR_Object obj_that_collided_with_me)
    {
        Debug.LogFormat("{0}: '{1}' colisiona conmigo.", Myself.Name, obj_that_collided_with_me.Name);
        colliding_object = obj_that_collided_with_me;
        current_state = AgentStateMachine();
    }

    public override void onGObjectCollision(ISSR_Object obj_that_collided_with_gripped_obj)
    {
        Debug.LogFormat("{0}: la piedra '{1}' que quiero agarrar colisiona conmigo.", Myself.Name, obj_that_collided_with_gripped_obj.Name);
        colliding_object = obj_that_collided_with_gripped_obj;
        current_state = AgentStateMachine();
    }

    public override void onDestArrived()
    {
        current_state = AgentStateMachine();
    }

    /*******************************************************
     *                FUNCIONES AUXILIARES                 *
     *******************************************************/

    public override void onEnterSensingArea(ISSR_Object obj) // Comienzo a ver objeto obj
    { // Acaba de entrar un objeto en el ‘campo visual’ del agente.
        object_just_seen = obj; // Anotamos objeto que vemos
        if (obj.type == ISSR_Type.SmallStone) // Si es una piedra pequeña
        {
            if (!Valid_Small_Stones.Contains((obj))) // si obj no está en la lista
            {
                Valid_Small_Stones.Add(obj); // Añadir obj a la lista
                Debug.LogFormat("{0}: nueva piedra pequeña '{1}', la anoto",
                Myself.Name, obj.Name);
            }
        }
        current_state = AgentStateMachine();
    }

    ISSRState ProcessCollision()  // Procesar colision 
    {
        ISSRState next_state = current_state;

        switch (current_state)
        {
            case ISSRState.GoingToGripSmallStone:
                // Guardamos el estado actual para recuperarlo mas adelante
                last_state = current_state;
                next_state = IrAPosicionSegura();
                break;
            case ISSRState.GoingToGoalWithSmallStone:
                // Guardamos el estado actual para recuperarlo mas adelante
                last_state = current_state;
                next_state = IrAPosicionSegura();
                break;
            case ISSRState.AvoidingObstacle:
                next_state = IrAPosicionSegura();
                break;

            default:
                Debug.LogErrorFormat("ProcessCollision() en {0}, estado {1} no considerado al colisionar", Myself.Name, current_state);
                break;
        }

        return next_state;
    }

    private ISSRState IrAPosicionSegura()
    {
        // Calculamos la posición segura
        ps = ISSRHelp.CalculateSafeLocation(this, colliding_object);
        // Pide la acción de ir a la posición segura
        acGotoLocation(ps);
        if (acCheckError()) // Si ha habido algún error en la acción
        {
            return ISSRState.Error; // Señala error
        }
        else
        { // En caso de que no haya error:
           return ISSRState.AvoidingObstacle; // cambio a estado siguiente
        }
    }

    ISSRState ResumeAfterCollision() // Versión de Práctica 3, completar en las siguientes
    { // Continuar con lo que se estaba haciendo en el momento de la colisión.
        ISSRState next_state = current_state;

        switch (last_state)  // Según estado anterior 
        {
            case ISSRState.GoingToGripSmallStone:
                next_state = ProcessStone();  // Volver a pedir coger piedra o ir a su lugar
                break;
            case ISSRState.GoingToGoalWithSmallStone:
                acGotoLocation(iMyGoalLocation());  // volver a pedir ir a la meta
                if (acCheckError())
                {
                    next_state = ISSRState.Error;
                }
                else
                {
                    next_state = ISSRState.GoingToGoalWithSmallStone;
                }
                break;
            default:
                Debug.LogErrorFormat("{0}, estado {1} no considerado al volver de colisión", Myself.Name, last_state);
                break;
        }
        return next_state;
    }

    /*******************************************************
     *              FIN FUNCIONES AUXILIARES               *
     *******************************************************/





}
